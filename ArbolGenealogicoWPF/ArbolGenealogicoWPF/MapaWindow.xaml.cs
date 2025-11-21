using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using ArbolGenealogico.Modelos;
using ArbolGenealogico.Geografia;

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
        private void DibujarMarcadores()
        {
            MapaCanvas.Children.Clear();

            if (MapaCanvas.ActualWidth <= 0 || MapaCanvas.ActualHeight <= 0)
                return;

            var miembros = ConvertirAFamiliaModelo();

            foreach (var miembro in miembros)
            {
                var (lat, lon) = miembro.CoordenadasResidencia;

                var punto = LatLonToPixel(lat, lon);

                // Marcador como un círculo
                var marker = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Cyan,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };

                // Tooltip con el nombre
                ToolTipService.SetToolTip(marker, miembro.Nombre);

                // Centramos el círculo en el punto
                Canvas.SetLeft(marker, punto.X - marker.Width / 2);
                Canvas.SetTop(marker, punto.Y - marker.Height / 2);

                MapaCanvas.Children.Add(marker);
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
