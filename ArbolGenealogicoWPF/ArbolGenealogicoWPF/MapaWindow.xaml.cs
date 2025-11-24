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
        private readonly ObservableCollection<MiembroFamilia> familiares;

        // Aproximación del rectángulo que cubre Costa Rica
        private const double MinLat = 8.0;
        private const double MaxLat = 11.5;
        private const double MinLon = -86.0;
        private const double MaxLon = -82.0;

        public MapaWindow(ObservableCollection<MiembroFamilia> ListaFamiliares)
        {
            InitializeComponent();
            familiares = ListaFamiliares;

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
            // Asumimos que la lista 'familiares' ya contiene MiembroFamilia válidos
            List<MiembroFamilia> miembros = ConvertirAFamiliaModelo();

            // Si se quiere eliminar por completo la validación de conteo, comentar la siguiente sección.
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
        //   (Simplificada: asumimos que 'familiares' ya contiene MiembroFamilia válidos)
        // ============================
        private List<MiembroFamilia> ConvertirAFamiliaModelo()
        {
            // Ya son MiembroFamilia: devolvemos una copia rápida
            return new List<MiembroFamilia>(familiares);
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

        private ToolTip CrearToolTipParaFamiliar(MiembroFamilia f)
        {
            // Asumimos que f no es null y que todas sus propiedades están inicializadas.
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

            // Edad: asumimos que la propiedad existe y tiene valor correcto
            panel.Children.Add(new TextBlock
            {
                Text = $"Edad: {f.Edad} años",
                FontSize = 14
            });

            // Mostrar coordenadas usando propiedades Numéricas (asumidas)
            panel.Children.Add(new TextBlock
            {
                Text = $"Coords: {f.Latitud.ToString(CultureInfo.InvariantCulture)},{f.Longitud.ToString(CultureInfo.InvariantCulture)}",
                FontSize = 14
            });

            // Fecha de nacimiento: usamos directamente (no nullable)
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
                // Asumimos que 'f' es MiembroFamilia válido y que tiene Latitud/Longitud
                double lat = f.Latitud;
                double lon = f.Longitud;

                var punto = LatLonToPixel(lat, lon);

                FrameworkElement marcadorVisual;

                // Intentamos usar la foto del familiar siempre (sin verificar File.Exists)
                Image img = new Image
                {
                    Width = 40,
                    Height = 40,
                    Stretch = Stretch.UniformToFill,
                };

                bool imageLoaded = false;
                try
                {
                    // Intento directo de cargar la imagen; si falla, caemos al fallback.
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(f.FotografiaRuta, UriKind.Absolute);
                    bitmap.EndInit();

                    img.Source = bitmap;
                    imageLoaded = true;
                }
                catch
                {
                    imageLoaded = false;
                }

                if (imageLoaded)
                {
                    // (Opcional) hacer el recorte circular del avatar
                    var clip = new EllipseGeometry(new Point(20, 20), 20, 20);
                    img.Clip = clip;
                    marcadorVisual = img;
                }
                else
                {
                    // Sin foto o falla al cargar -> usamos un punto circular como fallback
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
