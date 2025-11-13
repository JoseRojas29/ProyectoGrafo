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
    public partial class MainWindow : Window
    {
        // Colección principal de familiares
        private ObservableCollection<Familiar> _familiares = new ObservableCollection<Familiar>();

        // Ventanas auxiliares (nullable para evitar warnings)
        private EstadisticasWindow? _estadisticasWin;
        private MapaWindow? _mapaWin;

        public MainWindow()
        {
            InitializeComponent();

            FamiliaresList.ItemsSource = _familiares; // conectar la lista
        }

        // ===========================================
        //            EVENTOS DE BOTONES
        // ===========================================

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
                RutaFoto = _rutaFotoTemporal
            };

            // recalcular edad dentro de Familiar si corresponde
            _familiares.Add(f);
            LimpiarFormulario();
        }


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

        private string _rutaFotoTemporal = null;

        private void SeleccionarFoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                // opcional: copiar a carpeta del proyecto o dejar ruta absoluta
                _rutaFotoTemporal = dlg.FileName;

                // mostrar preview
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(_rutaFotoTemporal);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    FotoPreview.Source = bmp;
                }
                catch
                {
                    MessageBox.Show("No se pudo cargar la imagen seleccionada.");
                    _rutaFotoTemporal = null;
                    FotoPreview.Source = null;
                }
            }
        }


        private void LimpiarFormulario()
        {
            NombreInput.Text = "";
            CedulaInput.Text = "";
            FechaInput.Text = "";
            CoordenadasInput.Text = "";
            EdadInput.Text = "";
            _rutaFotoTemporal = null;
            FotoPreview.Source = null;
        }


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }



        private void AbrirEstadisticas_Click(object sender, RoutedEventArgs e)
        {
            if (_estadisticasWin == null || !_estadisticasWin.IsLoaded)
            {
                _estadisticasWin = new EstadisticasWindow(_familiares) { Owner = this };
                _estadisticasWin.Closed += (_, __) => _estadisticasWin = null;
            }

            _estadisticasWin.Show();
            _estadisticasWin.Activate();
        }

        private void AbrirMapa_Click(object sender, RoutedEventArgs e)
        {
            if (_mapaWin == null || !_mapaWin.IsLoaded)
            {
                _mapaWin = new MapaWindow(_familiares) { Owner = this };
                _mapaWin.Closed += (_, __) => _mapaWin = null;
            }

            _mapaWin.Show();
            _mapaWin.Activate();
        }

        // ============================================
        //             MÉTODOS AUXILIARES
        // ============================================

    }
}