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
            // Limpiamos el canvas sólo UNA VEZ antes de dibujar todo
            MapaCanvas.Children.Clear();

            // Primero dibujamos la ruta y distancias (queda abajo)
            DibujarRutaConDistancias();

            // Luego los marcadores encima
            DibujarMarcadores();
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


        /// <summary>
        /// Dibuja una polyline que une todos los familiares en el orden de la colección
        /// y añade etiquetas con la distancia entre todos los pares de nodos (i,j) con i<j.
        /// </summary>
        private void DibujarRutaConDistancias()
        {
            if (MapaCanvas.ActualWidth <= 0 || MapaCanvas.ActualHeight <= 0)
                return;

            // Obtener la lista de miembros con coordenadas válidas
            var lista = new List<MiembroFamilia>();
            foreach (var f in familiares)
            {
                if (f == null) continue;
                lista.Add(f);
            }

            if (lista.Count < 2)
                return; // nada que unir

            // Construir la Polyline en pixeles (orden de ingreso)
            var pts = new PointCollection();
            var pixelPoints = new List<Point>();
            foreach (var f in lista)
            {
                var p = LatLonToPixel(f.Latitud, f.Longitud);
                pts.Add(new System.Windows.Point(p.X, p.Y));
                pixelPoints.Add(p);
            }

            // Crear polyline principal
            var mainLine = new Polyline
            {
                Stroke = Brushes.LightGray,                  // color gris claro
                StrokeThickness = 1.5,                       // grosor similar al de las otras
                StrokeDashArray = new DoubleCollection { 4, 4 }, // patrón dash
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = 0.85,
                Points = pts
            };


            // Añadimos al canvas PRIMERO para que los marcadores queden encima
            MapaCanvas.Children.Add(mainLine);

            // Parámetros para las etiquetas y líneas entre TODOS los pares
            double labelOffsetPx = 10.0; // desplazamiento perpendicular base (ajustable)
            int n = pixelPoints.Count;

            // Recorremos todos los pares (i<j) para no duplicar
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var a = pixelPoints[i];
                    var b = pixelPoints[j];

                    // Dibujar una línea fina y discontinua entre a y b (opcional)
                    var pairLine = new Polyline
                    {
                        Stroke = Brushes.LightYellow,
                        StrokeThickness = 1.5,
                        StrokeDashArray = new DoubleCollection { 4, 4 },
                        Opacity = 0.85,
                        Points = new PointCollection { new System.Windows.Point(a.X, a.Y), new System.Windows.Point(b.X, b.Y) }
                    };
                    MapaCanvas.Children.Add(pairLine);

                    // calcular distancia real (metros) usando Haversine
                    double distMeters = HaversineDistance(lista[i].Latitud, lista[i].Longitud, lista[j].Latitud, lista[j].Longitud);

                    // formatear texto (m o km)
                    string distText = distMeters >= 1000 ? $"{(distMeters / 1000.0):F2} km" : $"{Math.Round(distMeters)} m";

                    // punto medio en pixeles
                    double midX = (a.X + b.X) / 2.0;
                    double midY = (a.Y + b.Y) / 2.0;

                    // perpendicular vector (-dy, dx) normalizado
                    double dx = b.X - a.X;
                    double dy = b.Y - a.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    double offsetX = 0;
                    double offsetY = 0;
                    if (len > 0.0001)
                    {
                        // Alternamos el lado para evitar solapamiento directo de etiquetas
                        int side = ((i + j) % 2 == 0) ? 1 : -1;
                        offsetX = -dy / len * labelOffsetPx * side;
                        offsetY = dx / len * labelOffsetPx * side;
                    }

                    // Crear etiqueta con Border + TextBlock (no interactiva)
                    var text = new TextBlock
                    {
                        Text = distText,
                        FontSize = 11,
                        Padding = new Thickness(4, 2, 4, 2),
                        Foreground = Brushes.Black,
                        TextWrapping = TextWrapping.NoWrap
                    };

                    var labelBorder = new Border
                    {
                        CornerRadius = new CornerRadius(4),
                        Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
                        BorderThickness = new Thickness(1),
                        Child = text,
                        IsHitTestVisible = false
                    };

                    // Medir tamaño para centrar correctamente (medimos el Border)
                    labelBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    var desired = labelBorder.DesiredSize;

                    // Posición final en canvas (centrada en el punto medio + offset perpendicular)
                    double left = midX + offsetX - desired.Width / 2.0;
                    double top = midY + offsetY - desired.Height / 2.0;

                    Canvas.SetLeft(labelBorder, left);
                    Canvas.SetTop(labelBorder, top);

                    MapaCanvas.Children.Add(labelBorder);
                }
            }
        }



        /// <summary>
        /// Calcula distancia Haversine en metros entre dos pares lat/lon.
        /// </summary>
        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0; // radio de la Tierra en metros
            double toRad = Math.PI / 180.0;
            double dLat = (lat2 - lat1) * toRad;
            double dLon = (lon2 - lon1) * toRad;
            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1 * toRad) * Math.Cos(lat2 * toRad) *
                       Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }


        private void DibujarMarcadores()
        {
            

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
            Rect imgRect = ObtenerRectImagenRenderizada();

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

        private Rect ObtenerRectImagenRenderizada()
        {
            double ctrlW = MapaImage.ActualWidth;
            double ctrlH = MapaImage.ActualHeight;

            if (!(MapaImage.Source is BitmapSource bmp) || ctrlW <= 0 || ctrlH <= 0)
                return new Rect(0, 0, ctrlW, ctrlH);

            // tamaño del bitmap en píxeles
            double bmpW = bmp.PixelWidth;
            double bmpH = bmp.PixelHeight;

            // scale según el Stretch (Uniform o UniformToFill)
            double scale = 1.0;
            if (MapaImage.Stretch == Stretch.Uniform)
                scale = Math.Min(ctrlW / bmpW, ctrlH / bmpH);
            else if (MapaImage.Stretch == Stretch.UniformToFill)
                scale = Math.Max(ctrlW / bmpW, ctrlH / bmpH);
            else
                scale = Math.Min(ctrlW / bmpW, ctrlH / bmpH); // fallback

            double renderW = bmpW * scale;
            double renderH = bmpH * scale;

            // offset (puede ser negativo cuando usamos UniformToFill)
            double offsetX = (ctrlW - renderW) / 2.0;
            double offsetY = (ctrlH - renderH) / 2.0;

            // Devolvemos el rect con coordenadas relativas al control (y por ende al Canvas si están alineados)
            return new Rect(offsetX, offsetY, renderW, renderH);
        }

    }
}
