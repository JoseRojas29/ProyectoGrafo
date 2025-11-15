using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace ArbolGenealogicoWPF
{
    public partial class MainWindow : WindowBaseLogica
    {
        // Colección principal de familiares (por ahora lista, luego grafo)
        private ObservableCollection<Familiar> familiares = new ObservableCollection<Familiar>();

        // Ventanas auxiliares (nullable para evitar warnings)
        private EstadisticasWindow? estadisticasWin;
        private MapaWindow? mapaWin;

        // Ruta temporal de la foto seleccionada
        private string? rutaFotoTemporal;

        public MainWindow()
        {
            InitializeComponent();
            FamiliaresList.ItemsSource = familiares; // conectar la lista
        }

        // ===========================================
        //            EVENTOS DE BOTONES
        // ===========================================

        private void SeleccionarFoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                rutaFotoTemporal = dlg.FileName;

                // Mostrar preview de la foto
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(rutaFotoTemporal);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    FotoPreview.Source = bmp;
                }
                catch
                {
                    var errorWin = new ErrorWindow("No se pudo cargar la imagen seleccionada.");
                    errorWin.Owner = this; // Para que se centre sobre la ventana principal
                    errorWin.ShowDialog(); // ShowDialog bloquea hasta que se cierre
                    rutaFotoTemporal = null;
                    FotoPreview.Source = null;
                }
            }
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreInput.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            // Crear nuevo familiar
            var f = new Familiar
            {
                Nombre = NombreInput.Text.Trim(),
                Cedula = CedulaInput.Text.Trim(),
                Coordenadas = CoordenadasInput.Text.Trim(),
                FechaNacimiento = DateTime.TryParse(FechaInput.Text, out var fechaVal) ? fechaVal : (DateTime?)null,
                RutaFoto = rutaFotoTemporal
            };

            // recalcular edad dentro de Familiar si corresponde
            familiares.Add(f);
            LimpiarFormulario();
        }

        // Eliminar familiar seleccionado
        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (FamiliaresList.SelectedItem is Familiar sel)
            {
                _familiares.Remove(sel);
            }
            else
            {
                MessageBox.Show("Selecciona un familiar en la lista para eliminar.");
            }
        }

        private void AbrirEstadisticas_Click(object sender, RoutedEventArgs e)
        {
            if (estadisticasWin == null || !estadisticasWin.IsLoaded)
            {
                estadisticasWin = new EstadisticasWindow(familiares) { Owner = this };
                estadisticasWin.Closed += (_, __) => estadisticasWin = null;
            }

            estadisticasWin.Show();
            estadisticasWin.Activate();
        }

        private void AbrirMapa_Click(object sender, RoutedEventArgs e)
        {
            if (mapaWin == null || !mapaWin.IsLoaded)
            {
                mapaWin = new MapaWindow(familiares) { Owner = this };
                mapaWin.Closed += (_, __) => _mapaWin = null;
            }

            mapaWin.Show();
            mapaWin.Activate();
        }

        // ============================================
        //             MÉTODOS AUXILIARES
        // ============================================

        private void LimpiarFormulario()
        {
            NombreInput.Text = "";
            CedulaInput.Text = "";
            FechaInput.Text = "";
            CoordenadasInput.Text = "";
            EdadInput.Text = "";
            ParentescoInput.Text = "";
            rutaFotoTemporal = null;
            FotoPreview.Source = null;
        }
    }
}