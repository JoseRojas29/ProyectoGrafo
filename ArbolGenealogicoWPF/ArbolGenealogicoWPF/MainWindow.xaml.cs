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
    using Microsoft.Win32;
    using System.Collections.ObjectModel;

    public partial class MainWindow : Window
    {
        private ObservableCollection<Familiar> _familiares = new ObservableCollection<Familiar>();

        public MainWindow()
        {
            InitializeComponent();
            
            FamiliaresList.ItemsSource = _familiares; // conectar la lista
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones simples
            if (string.IsNullOrWhiteSpace(NombreText.Text))
            {
                MessageBox.Show("El nombre es obligatorio.");
                return;
            }

            double? lat = null, lon = null;
            if (double.TryParse(LatText.Text, out var latVal)) lat = latVal;
            if (double.TryParse(LonText.Text, out var lonVal)) lon = lonVal;

            var f = new Familiar
            {
                Nombre = NombreText.Text.Trim(),
                FechaNacimiento = DateTime.TryParse(FechaText.Text, out var fechaVal) ? fechaVal : null,
                Pais = PaisText.Text.Trim(),
                Ciudad = CiudadText.Text.Trim(),
                Latitud = lat,
                Longitud = lon,
                FotoUri = string.IsNullOrWhiteSpace(FotoText.Text) ? null : FotoText.Text.Trim()
            };

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

        private void SeleccionarFoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp"
            };
            if (dlg.ShowDialog() == true)
            {
                FotoText.Text = dlg.FileName;
            }
        }

        private void LimpiarFormulario()
        {
            NombreText.Text = "";
            FechaText.Text = "";
            PaisText.Text = "";
            CiudadText.Text = "";
            LatText.Text = "";
            LonText.Text = "";
            FotoText.Text = "";
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

    }
}