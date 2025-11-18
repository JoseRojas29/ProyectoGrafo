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

        // Cédula numérica sin signos ni espacios
        private int cedulaValida;

        // Fecha de nacimiento validada
        private DateTime? fechaNacimiento;

        // Edad validada
        private int edadValida;

        // Lista de parentescos válidos (primer grado de consanguinidad)
        private readonly string[] parentescosValidos = {"padre", "madre", "esposo", "esposa", "hijo", "hija", "hermano", "hermana"};

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
                    var errorWin = new ErroresWindow("No se pudo cargar la imagen seleccionada.");
                    errorWin.Owner = this; // Para que se centre sobre la ventana principal
                    errorWin.ShowDialog(); // ShowDialog bloquea hasta que se cierre
                    rutaFotoTemporal = null;
                    FotoPreview.Source = null;
                }
            }
        }
        
        private void CrearArbol_Click(object sender, RoutedEventArgs e)
        {
            if (!VerificarDatos(1))
                return;

            // Crear nuevo familiar
            var f = new Familiar
            {
                Nombre = NombreInput.Text.Trim(),
                Cedula = cedulaValida,
                Coordenadas = CoordenadasInput.Text.Trim(),
                FechaNacimiento = fechaNacimiento,
                Edad = edadValida,
                RutaFoto = rutaFotoTemporal
            };

            familiares.Add(f);
            LimpiarFormulario();
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            if (!VerificarDatos(2))
                return;

            // Crear nuevo familiar
            var f = new Familiar
            {
                Nombre = NombreInput.Text.Trim(),
                Cedula = cedulaValida,
                Coordenadas = CoordenadasInput.Text.Trim(),
                FechaNacimiento = fechaNacimiento,
                Edad = edadValida,
                RutaFoto = rutaFotoTemporal
            };

            familiares.Add(f);
            LimpiarFormulario();
        }

        // Eliminar familiar seleccionado
        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (FamiliaresList.SelectedItem is Familiar sel)
            {
                familiares.Remove(sel);
            }
            else
            {
                var errorWin = new ErroresWindow("Selecciona un familiar en la lista para eliminar.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
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
                mapaWin.Closed += (_, __) => mapaWin = null;
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
            rutaFotoTemporal = null;
            FotoPreview.Source = null;
        }

        private bool VerificarDatos(int modo)
        {
            // Nombre obligatorio
            if (string.IsNullOrWhiteSpace(NombreInput.Text))
            {
                var errorWin = new ErroresWindow("Por favor ingrese un nombre.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            // Cédula obligatoria
            if (string.IsNullOrWhiteSpace(CedulaInput.Text))
            {
                var errorWin = new ErroresWindow("Por favor ingrese una cédula.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            // Normalizar: quitar espacios, guiones, comas y puntos
            string cedulaNormalizada = CedulaInput.Text.Replace(" ", "").Replace("-", "").Replace(",", "").Replace(".", "");

            // Intentar convertir a entero
            if (!int.TryParse(cedulaNormalizada, out int cedulaNumerica))
            {
                var errorWin = new ErroresWindow("La cédula debe contener solo números.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            // Guardar valor ya convertido para asignarlo
            cedulaValida = cedulaNumerica;

            // Fecha obligatoria y válida
            if (string.IsNullOrWhiteSpace(FechaInput.Text) || !DateTime.TryParse(FechaInput.Text, out DateTime fechaValidada))
            {
                var errorWin = new ErroresWindow("Por favor ingrese una fecha válida.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            fechaNacimiento = fechaValidada;

            // Coordenadas obligatorias (luego se puede hacer una verificación para ver si son válidas en el sistema)
            if (string.IsNullOrWhiteSpace(CoordenadasInput.Text))
            {
                var errorWin = new ErroresWindow("Por favor ingrese las coordenadas.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            // Edad obligatoria y válida
            if (string.IsNullOrWhiteSpace(EdadInput.Text) || !int.TryParse(EdadInput.Text, out int edad) || edad <= 0)
            {
                var errorWin = new ErroresWindow("Por favor ingrese una edad válida.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            edadValida = edad;

            

            // Foto obligatoria
            if (rutaFotoTemporal == null || FotoPreview.Source == null)
            {
                var errorWin = new ErroresWindow("Por favor ingrese una foto de la persona.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return false;
            }

            return true;
        }
    }
}