using ArbolGenealogico.Geografia;
using ArbolGenealogico.Modelos;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace ArbolGenealogicoWPF
{
    public partial class MapaWindow : WindowBaseLogica
    {
        private readonly ObservableCollection<Familiar> familiares;

        // Aproximación del rectángulo que cubre Costa Rica
        private const double MinLat = 8.0;
        private const double MaxLat = 11.5;
        private const double MinLon = -86.0;
        private const double MaxLon = -82.0;

        public MapaWindow(ObservableCollection<Familiar> listaFamiliares)
        {
            InitializeComponent();
            familiares = listaFamiliares;

            // Esperamos a que el layout esté listo para tener tamaños reales del Canvas
            Loaded += MapaWindow_Loaded;
        }

        private void MapaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Mostrar estadísticas usando tu grafo
            CalcularYMostrarEstadisticasMapa();

            // 2. Dibujar los marcadores sobre el mapa
            DibujarMarcadores();
        }

        // ============================
        //   ESTADÍSTICAS (similar a EstadisticasWindow)
        // ============================
        private void CalcularYMostrarEstadisticasMapa()
        {
            List<MiembroFamilia> miembros = ConvertirAFamiliaModelo();

            if (miembros.Count < 2)
            {
                string msg = "Se requieren al menos 2 familiares con coordenadas válidas.";
                Farthest.Text = msg;
                Closest.Text = msg;
                Promedio.Text = "-";
                return;
            }

            var grafo = new GrafoGeografico(miembros);

            var parCercano = grafo.ObtenerParMasCercano();
            if (parCercano != null)
            {
                Closest.Text =
                    $"Más cercanos: {parCercano.Value.A.Nombre} y {parCercano.Value.B.Nombre} " +
                    $"- {parCercano.Value.DistanciaKm:F2} km";
            }
            else
            {
                Closest.Text = "No hay suficientes datos para el par más cercano.";
            }

            var parLejano = grafo.ObtenerParMasLejano();
            if (parLejano != null)
            {
                Farthest.Text =
                    $"Más lejanos: {parLejano.Value.A.Nombre} y {parLejano.Value.B.Nombre} " +
                    $"- {parLejano.Value.DistanciaKm:F2} km";
            }
            else
            {
                Farthest.Text = "No hay suficientes datos para el par más lejano.";
            }

            double promedioKm = grafo.ObtenerDistanciaPromedio();
            Promedio.Text = $"Promedio: {promedioKm:F2} km";
        }

        // ============================
        //   CONVERSIÓN FAMILIAR -> MiembroFamilia
        // ============================
        private List<MiembroFamilia> ConvertirAFamiliaModelo()
        {
            var lista = new List<MiembroFamilia>();

            foreach (var f in familiares)
            {
                if (f == null)
                    continue;

                if (!TryParseCoordenadas(f.Coordenadas, out double lat, out double lon))
                    continue;

                DateTime fecha = DateTime.Now;

                var miembro = new MiembroFamilia(
                    nombre: f.Nombre ?? string.Empty,
                    cedula: f.Cedula.ToString(),
                    fechaNacimiento: fecha,
                    estaVivo: true,
                    fotografiaRuta: f.RutaFoto ?? string.Empty,
                    latitud: lat,
                    longitud: lon
                );

                lista.Add(miembro);
            }

            return lista;
        }

        private bool TryParseCoordenadas(string? texto, out double latitud, out double longitud)
        {
            latitud = 0;
            longitud = 0;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            var partes = texto.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2)
                return false;

            var style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;

            if (!double.TryParse(partes[0].Trim(), style, culture, out latitud))
                return false;

            if (!double.TryParse(partes[1].Trim(), style, culture, out longitud))
                return false;

            return true;
        }

        // ============================
        //   MAPEO LAT/LON -> PIXEL Y DIBUJO DE MARCADORES
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

        private ToolTip CrearToolTipParaFamiliar(Familiar f)
        {
            // Aquí decides qué info mostrar
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

            if (!string.IsNullOrWhiteSpace(f.Coordenadas))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"Coords: {f.Coordenadas}",
                    FontSize = 14
                });
            }

            if (f.FechaNacimiento.HasValue)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"Nac: {f.FechaNacimiento.Value:dd/MM/yyyy}",
                    FontSize = 14
                });
            }

            return new ToolTip { Content = panel };
        }

        private void DibujarMarcadores()
        {
            MapaCanvas.Children.Clear();

            if (MapaCanvas.ActualWidth <= 0 || MapaCanvas.ActualHeight <= 0)
                return;

            foreach (var f in familiares)
            {
                if (f == null)
                    continue;

                // Si no tiene coordenadas válidas, lo ignoramos
                if (!TryParseCoordenadas(f.Coordenadas, out double lat, out double lon))
                    continue;

                var punto = LatLonToPixel(lat, lon);

                FrameworkElement marcadorVisual;

                // Intentamos usar la foto del familiar si existe
                if (!string.IsNullOrWhiteSpace(f.RutaFoto) && File.Exists(f.RutaFoto))
                {
                    var img = new Image
                    {
                        Width = 40,
                        Height = 40,
                        Stretch = Stretch.UniformToFill,
                    };

                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(f.RutaFoto, UriKind.Absolute);
                        bitmap.EndInit();

                        img.Source = bitmap;
                    }
                    catch
                    {
                        // Si falla la carga de la imagen, caeremos al marcador por defecto
                        img = null;
                    }

                    if (img != null)
                    {
                        // (Opcional) hacer el recorte circular del avatar
                        var clip = new EllipseGeometry(new Point(20, 20), 20, 20);
                        img.Clip = clip;

                        marcadorVisual = img;
                    }
                    else
                    {
                        marcadorVisual = CrearMarcadorCircularFallback();
                    }
                }
                else
                {
                    // Sin foto -> usamos un punto circular como fallback
                    marcadorVisual = CrearMarcadorCircularFallback();
                }

                // Tooltip con la info del familiar
                ToolTipService.SetToolTip(marcadorVisual, CrearToolTipParaFamiliar(f));

                // Centramos el marcador en el punto calculado
                double ancho = marcadorVisual.Width;
                double alto = marcadorVisual.Height;
                Canvas.SetLeft(marcadorVisual, punto.X - ancho / 2);
                Canvas.SetTop(marcadorVisual, punto.Y - alto / 2);

                MapaCanvas.Children.Add(marcadorVisual);
            }
        }


        private Point LatLonToPixel(double lat, double lon)
        {
            double width = MapaCanvas.ActualWidth;
            double height = MapaCanvas.ActualHeight;

            // Normalizar longitudes (oeste a este → 0..1)
            double xNorm = (lon - MinLon) / (MaxLon - MinLon);
            // Normalizar latitudes (sur a norte → 0..1, invertido porque y crece hacia abajo)
            double yNorm = 1.0 - (lat - MinLat) / (MaxLat - MinLat);

            // Clamp por seguridad
            xNorm = Math.Clamp(xNorm, 0.0, 1.0);
            yNorm = Math.Clamp(yNorm, 0.0, 1.0);

            double x = xNorm * width;
            double y = yNorm * height;

            return new Point(x, y);
        }
    }
}
