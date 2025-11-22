using ArbolGenealogico.Geografia; // para GrafoGeografico (si está en este namespace)
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArbolGenealogicoWPF
{
    public partial class MapaWindow : WindowBaseLogica
    {
        // Ahora trabajamos directamente con la clase MiembroFamilia que pusiste (namespace ArbolGenealogicoWPF)
        private readonly ObservableCollection<MiembroFamilia> familiares;

        // Límites del mapa (se calculan según los datos)
        private double MinLat = -90.0;
        private double MaxLat = 90.0;
        private double MinLon = -180.0;
        private double MaxLon = 180.0;

        public MapaWindow(ObservableCollection<MiembroFamilia> listaFamiliares)
        {
            InitializeComponent();

            // asignación correcta (case-sensitive)
            familiares = listaFamiliares ?? new ObservableCollection<MiembroFamilia>();

            Loaded += MapaWindow_Loaded;
        }

        private void MapaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CalcularYMostrarEstadisticasMapa();
            DibujarMarcadores();
        }

        // ============================
        // Estadísticas
        // ============================
        private void CalcularYMostrarEstadisticasMapa()
        {
            // GrafoGeografico probablemente espera un tipo concreto (de ArbolGenealogico.Geografia).
            // Intentamos convertir a la clase que espera (si es necesario) usando reflexión tolerante.
            var miembrosGeograficos = ConvertirAFamiliaGeograficaDesdeNodos();

            if (miembrosGeograficos.Count < 2)
            {
                string msg = "Se requieren al menos 2 familiares con coordenadas válidas.";
                Farthest.Text = msg;
                Closest.Text = msg;
                Promedio.Text = "-";
                return;
            }

            // Actualizar límites del mapa según los miembros detectados
            ActualizarLimitesMapa(miembrosGeograficos);

            // Construye el grafo geográfico (si GrafoGeografico requiere el tipo del namespace Geografia)
            var grafo = new GrafoGeografico(miembrosGeograficos);

            var parCercano = grafo.ObtenerParMasCercano();
            if (parCercano != null)
            {
                Closest.Text =
                    $"Más cercanos: {parCercano.Value.A.Nombre} y {parCercano.Value.B.Nombre} - {parCercano.Value.DistanciaKm:F2} km";
            }
            else
            {
                Closest.Text = "No hay suficientes datos para el par más cercano.";
            }

            var parLejano = grafo.ObtenerParMasLejano();
            if (parLejano != null)
            {
                Farthest.Text =
                    $"Más lejanos: {parLejano.Value.A.Nombre} y {parLejano.Value.B.Nombre} - {parLejano.Value.DistanciaKm:F2} km";
            }
            else
            {
                Farthest.Text = "No hay suficientes datos para el par más lejano.";
            }

            double promedioKm = grafo.ObtenerDistanciaPromedio();
            Promedio.Text = $"Promedio: {promedioKm:F2} km";
        }

        // ============================
        // Conversión: ArbolGenealogicoWPF.MiembroFamilia -> tipo que usa GrafoGeografico
        // (tolerante a firmas distintas usando reflexión)
        // ============================
        private List<object> ConvertirAFamiliaGeograficaDesdeNodos()
        {
            var lista = new List<object>();

            // obtenemos el tipo objetivo que GrafoGeografico espera: si su constructor pide List<MiembroFamilia>
            // asumimos que el tipo se llama "MiembroFamilia" en algún namespace diferente (p. ej. ArbolGenealogico.Geografia o Modelos).
            // Buscamos en los assemblies cargados un tipo público con ese nombre (excluyendo el actual)
            Type? tipoDestino = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypesSafe())
                .FirstOrDefault(t => t.Name == "MiembroFamilia" && t != typeof(MiembroFamilia));

            // Si no encontramos un tipo destino distinto, asumimos que GrafoGeografico acepta nuestro tipo actual.
            if (tipoDestino == null)
            {
                // Devolver instancias del propio tipo (se usan como objeto). GrafoGeografico puede aceptar estas instancias si su definición coincide.
                return familiares.Select(f => (object)f).ToList();
            }

            // Si sí hay un tipo externo "MiembroFamilia", intentamos instanciar objetos de ese tipo copiando propiedades desde nuestros nodos
            foreach (var f in familiares)
            {
                if (f == null) continue;

                double lat = f.CoordenadasResidencia.Latitud;
                double lon = f.CoordenadasResidencia.Longitud;

                // Si coordenadas son 0,0 y se considera inválido, saltamos (si tú consideras distinto, ajusta la condición)
                // Si usas string de coordenadas en otra parte, cambia la lógica
                // Aquí asumimos que (0,0) es posible, por lo que no filtramos por eso.

                object? creado = CrearInstanciaMiembroGeografico(tipoDestino, f, lat, lon);
                if (creado != null)
                    lista.Add(creado);
            }

            return lista;
        }

        // Crea una instancia del tipo destino copiando campos posibles. Intenta varias firmas de constructor comunes.
        private object? CrearInstanciaMiembroGeografico(Type tipoDestino, MiembroFamilia fuente, double lat, double lon)
        {
            try
            {
                // Intento 1: constructor (string nombre, int cedula, bool? estaVivo, int? edad, DateTime fechaNacimiento, string fotografiaRuta, double latitud, double longitud)
                var ctor1 = tipoDestino.GetConstructor(new Type[] {
                    typeof(string), typeof(int), typeof(bool?), typeof(int?), typeof(DateTime), typeof(string), typeof(double), typeof(double)
                });
                if (ctor1 != null)
                {
                    object? obj = ctor1.Invoke(new object[] {
                        fuente.Nombre ?? string.Empty,
                        fuente.Cedula,
                        (bool?)fuente.EstaVivo,
                        fuente.Edad,
                        fuente.FechaNacimiento,
                        fuente.FotografiaRuta ?? string.Empty,
                        lat,
                        lon
                    });
                    return obj;
                }

                // Intento 2: constructor (string nombre, int cedula, bool estaVivo, int? edad, DateTime fechaNacimiento, string fotografiaRuta, double latitud, double longitud)
                var ctor2 = tipoDestino.GetConstructor(new Type[] {
                    typeof(string), typeof(int), typeof(bool), typeof(int?), typeof(DateTime), typeof(string), typeof(double), typeof(double)
                });
                if (ctor2 != null)
                {
                    object? obj = ctor2.Invoke(new object[] {
                        fuente.Nombre ?? string.Empty,
                        fuente.Cedula,
                        fuente.EstaVivo,
                        fuente.Edad,
                        fuente.FechaNacimiento,
                        fuente.FotografiaRuta ?? string.Empty,
                        lat,
                        lon
                    });
                    return obj;
                }

                // Intento 3: constructor (string nombre, int cedula, DateTime fechaNacimiento, bool? estaVivo, string fotografiaRuta, double latitud, double longitud)
                var ctor3 = tipoDestino.GetConstructor(new Type[] {
                    typeof(string), typeof(int), typeof(DateTime), typeof(bool?), typeof(string), typeof(double), typeof(double)
                });
                if (ctor3 != null)
                {
                    object? obj = ctor3.Invoke(new object[] {
                        fuente.Nombre ?? string.Empty,
                        fuente.Cedula,
                        fuente.FechaNacimiento,
                        (bool?)fuente.EstaVivo,
                        fuente.FotografiaRuta ?? string.Empty,
                        lat,
                        lon
                    });
                    return obj;
                }

                // Intento 4: constructor mínimo (string nombre, double latitud, double longitud)
                var ctorMin = tipoDestino.GetConstructor(new Type[] { typeof(string), typeof(double), typeof(double) });
                if (ctorMin != null)
                {
                    object? obj = ctorMin.Invoke(new object[] { fuente.Nombre ?? "(sin nombre)", lat, lon });
                    return obj;
                }

                // Intento 5: constructor vacío + setear propiedades si existen
                var ctorDefault = tipoDestino.GetConstructor(Type.EmptyTypes);
                if (ctorDefault != null)
                {
                    var inst = ctorDefault.Invoke(null);
                    TrySetIfExists(tipoDestino, inst, "Nombre", fuente.Nombre);
                    TrySetIfExists(tipoDestino, inst, "Cedula", fuente.Cedula);
                    TrySetIfExists(tipoDestino, inst, "FechaNacimiento", fuente.FechaNacimiento);
                    TrySetIfExists(tipoDestino, inst, "EstaVivo", fuente.EstaVivo);
                    TrySetIfExists(tipoDestino, inst, "FotografiaRuta", fuente.FotografiaRuta);
                    TrySetIfExists(tipoDestino, inst, "Latitud", lat);
                    TrySetIfExists(tipoDestino, inst, "Longitud", lon);
                    TrySetIfExists(tipoDestino, inst, "CoordenadasResidencia", (lat, lon)); // si la clase destino tiene también tuple
                    return inst;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando instancia geográfica: {ex.Message}");
            }

            return null;
        }

        private void TrySetIfExists(Type tipo, object target, string propName, object? value)
        {
            try
            {
                var prop = tipo.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop != null && value != null)
                {
                    // Convert.ChangeType puede fallar para nullable y tuples; usamos asignación directa si el tipo es compatible
                    if (prop.PropertyType.IsAssignableFrom(value.GetType()))
                    {
                        prop.SetValue(target, value);
                    }
                    else
                    {
                        try
                        {
                            var converted = Convert.ChangeType(value, prop.PropertyType);
                            prop.SetValue(target, converted);
                        }
                        catch { /* ignorar conversiones que no coinciden */ }
                    }
                }
            }
            catch { /* ignorar */ }
        }

        // ============================
        // DIBUJO DE MARCADORES
        // ============================
        private FrameworkElement CrearMarcadorCircularFallback()
        {
            return new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Cyan,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
        }

        private ToolTip CrearToolTipParaFamiliar(MiembroFamilia f)
        {
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = f.Nombre ?? "(Sin nombre)",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Cédula: {f.Cedula}",
                FontSize = 14
            });

            if (f.Edad.HasValue)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"Edad: {f.Edad.Value} años",
                    FontSize = 14
                });
            }

            panel.Children.Add(new TextBlock
            {
                Text = $"Coords: {f.CoordenadasResidencia.Latitud.ToString(CultureInfo.InvariantCulture)}, {f.CoordenadasResidencia.Longitud.ToString(CultureInfo.InvariantCulture)}",
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Nac: {f.FechaNacimiento:dd/MM/yyyy}",
                FontSize = 14
            });

            return new ToolTip { Content = panel };
        }

        private void DibujarMarcadores()
        {
            MapaCanvas.Children.Clear();

            if (MapaCanvas.ActualWidth <= 0 || MapaCanvas.ActualHeight <= 0)
                return;

            foreach (var f in familiares)
            {
                if (f == null) continue;

                double lat = f.CoordenadasResidencia.Latitud;
                double lon = f.CoordenadasResidencia.Longitud;

                // Si necesitas filtrar coordenadas inválidas: add your logic here

                var punto = LatLonToPixel(lat, lon);

                FrameworkElement marcadorVisual;

                if (!string.IsNullOrWhiteSpace(f.FotografiaRuta) && File.Exists(f.FotografiaRuta))
                {
                    Image img = null;
                    try
                    {
                        img = new Image
                        {
                            Width = 40,
                            Height = 40,
                            Stretch = Stretch.UniformToFill,
                        };

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(f.FotografiaRuta, UriKind.Absolute);
                        bitmap.EndInit();

                        img.Source = bitmap;

                        var clip = new EllipseGeometry(new Point(20, 20), 20, 20);
                        img.Clip = clip;
                    }
                    catch
                    {
                        img = null;
                    }

                    marcadorVisual = img ?? CrearMarcadorCircularFallback();
                }
                else
                {
                    marcadorVisual = CrearMarcadorCircularFallback();
                }

                ToolTipService.SetToolTip(marcadorVisual, CrearToolTipParaFamiliar(f));

                double ancho = marcadorVisual.Width;
                double alto = marcadorVisual.Height;
                Canvas.SetLeft(marcadorVisual, punto.X - ancho / 2);
                Canvas.SetTop(marcadorVisual, punto.Y - alto / 2);

                MapaCanvas.Children.Add(marcadorVisual);
            }
        }

        // ============================
        // Lat/Lon -> Pixel
        // ============================
        private Point LatLonToPixel(double lat, double lon)
        {
            double width = MapaCanvas.ActualWidth;
            double height = MapaCanvas.ActualHeight;

            double denomLon = (MaxLon - MinLon);
            double xNorm = denomLon == 0 ? 0.5 : (lon - MinLon) / denomLon;

            double denomLat = (MaxLat - MinLat);
            double yNorm = denomLat == 0 ? 0.5 : 1.0 - (lat - MinLat) / denomLat;

            xNorm = Math.Clamp(xNorm, 0.0, 1.0);
            yNorm = Math.Clamp(yNorm, 0.0, 1.0);

            return new Point(xNorm * width, yNorm * height);
        }

        // Calcula límites a partir de la lista geográfica (objetos genericos que tienen lat/long)
        private void ActualizarLimitesMapa(List<object> miembros)
        {
            if (miembros == null || miembros.Count == 0) return;

            double minLat = double.MaxValue, maxLat = double.MinValue;
            double minLon = double.MaxValue, maxLon = double.MinValue;

            foreach (var m in miembros)
            {
                // intentamos leer Latitud/Longitud con reflexión por varios nombres posibles
                if (TryGetDouble(m, "Latitud", out double lat) || TryGetDouble(m, "Latitude", out lat) || TryGetDouble(m, "Lat", out lat))
                {
                    minLat = Math.Min(minLat, lat);
                    maxLat = Math.Max(maxLat, lat);
                }

                if (TryGetDouble(m, "Longitud", out double lon) || TryGetDouble(m, "Longitude", out lon) || TryGetDouble(m, "Lon", out lon))
                {
                    minLon = Math.Min(minLon, lon);
                    maxLon = Math.Max(maxLon, lon);
                }

                // también soportamos la tupla CoordenadasResidencia
                var tprop = m.GetType().GetProperty("CoordenadasResidencia", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (tprop != null)
                {
                    var val = tprop.GetValue(m);
                    if (val != null)
                    {
                        // si es ValueTuple<double,double>
                        var latProp = val.GetType().GetField("Latitud") ?? val.GetType().GetField("Item1");
                        var lonProp = val.GetType().GetField("Longitud") ?? val.GetType().GetField("Item2");
                        if (latProp != null && lonProp != null)
                        {
                            var lv = latProp.GetValue(val);
                            var lv2 = lonProp.GetValue(val);
                            if (double.TryParse(lv?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat2) &&
                                double.TryParse(lv2?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon2))
                            {
                                minLat = Math.Min(minLat, lat2);
                                maxLat = Math.Max(maxLat, lat2);
                                minLon = Math.Min(minLon, lon2);
                                maxLon = Math.Max(maxLon, lon2);
                            }
                        }
                    }
                }
            }

            if (minLat == double.MaxValue || minLon == double.MaxValue)
            {
                // fallback global
                MinLat = -90; MaxLat = 90; MinLon = -180; MaxLon = 180;
                return;
            }

            double latPad = (maxLat - minLat) * 0.05;
            double lonPad = (maxLon - minLon) * 0.05;

            MinLat = minLat - latPad;
            MaxLat = maxLat + latPad;
            MinLon = minLon - lonPad;
            MaxLon = maxLon + lonPad;

            if (MinLat == MaxLat) { MinLat -= 0.01; MaxLat += 0.01; }
            if (MinLon == MaxLon) { MinLon -= 0.01; MaxLon += 0.01; }
        }

        // intento robusto de lectura double via reflexión
        private bool TryGetDouble(object obj, string propName, out double result)
        {
            result = 0;
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop == null) return false;
            var val = prop.GetValue(obj);
            if (val == null) return false;
            if (val is double d) { result = d; return true; }
            if (val is float f) { result = f; return true; }
            if (val is decimal dec) { result = (double)dec; return true; }
            if (double.TryParse(val.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)) { result = parsed; return true; }
            return false;
        }
    }

    // Extension helper para evitar excepciones al enumerar tipos de assembly
    internal static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetTypesSafe(this Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch { return Enumerable.Empty<Type>(); }
        }
    }
}
