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
using System.Globalization;

namespace ArbolGenealogicoWPF
{
    public partial class MainWindow : WindowBaseLogica
    {
        // Colección principal de familiares (por ahora lista, luego grafo)
        private ObservableCollection<MiembroFamilia> familiares = new ObservableCollection<MiembroFamilia>();

        // Ventanas auxiliares (nullable para evitar warnings)
        private EstadisticasWindow? estadisticasWin;
        private MapaWindow? mapaWin;

        // Ruta temporal de la foto seleccionada
        private string? rutaFotoTemporal;

        // Cédula numérica sin signos ni espacios
        private int cedulaValida;

        // Fecha de nacimiento validada
        private DateTime fechaNacimiento;

        // Edad validada
        private int edadValida;

        // Verificación constante del checkbox
        private bool estaVivo = true;

        // Diccionario para evitar duplicados
        private Dictionary<int, MiembroFamilia> indicePorCedula = new();

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

        private void MuerteCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            estaVivo = false;
        }

        private void MuerteCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            estaVivo = true;
        }

        private void CrearArbol_Click(object sender, RoutedEventArgs e)
        {
            // 0. Avisar al usuario que se perderán datos si continúa
            var confirmWin = new ConfirmacionWindow();
            confirmWin.Owner = this;
            confirmWin.ShowDialog();

            if (!confirmWin.Confirmado)
                return;

            // 1. Verificar validez de datos del primer miembro
            if (!VerificarDatos(1))
                return;

            // 2. Limpiar estructuras previas
            familiares.Clear();
            indicePorCedula.Clear();

            // 3. Limpiar interfaz gráfica
            // Ejemplo si usás un Canvas:
            // ArbolCanvas.Children.Clear();
            // O si usás un TreeView:
            // ArbolTreeView.Items.Clear();

            // 4. Agregar el primer miembro (raíz del árbol)
            var coords = ParsearCoordenadas(CoordenadasInput.Text.Trim());

            MiembroFamilia f;

            if (estaVivo)
            {
                f = new MiembroFamilia(
                    NombreInput.Text.Trim(),
                    cedulaValida,
                    estaVivo,
                    null,
                    fechaNacimiento,
                    rutaFotoTemporal,
                    coords.Latitud,
                    coords.Longitud
                );
            }
            else
            {
                f = new MiembroFamilia(
                    NombreInput.Text.Trim(),
                    cedulaValida,
                    estaVivo,
                    edadValida,
                    fechaNacimiento,
                    rutaFotoTemporal,
                    coords.Latitud,
                    coords.Longitud
                );

            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // Acá también se actualiza la interfaz gráfica según la implementación elegida
            // Pero sigue pendiente
            // Pero es como tal el dibujo del árbol

            LimpiarFormulario();
            }
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Verificar validez de datos del primer miembro
            if (!VerificarDatos(2))
                return;

            // 2. Crear el nuevo miembro
            var coords = ParsearCoordenadas(CoordenadasInput.Text.Trim());

            MiembroFamilia f;

            if (estaVivo)
            {
                f = new MiembroFamilia(
                    NombreInput.Text.Trim(),
                    cedulaValida,
                    estaVivo,
                    null,
                    fechaNacimiento,
                    rutaFotoTemporal,
                    coords.Latitud,
                    coords.Longitud
                );
            }
            else
            {
                f = new MiembroFamilia(
                    NombreInput.Text.Trim(),
                    cedulaValida,
                    estaVivo,
                    edadValida,
                    fechaNacimiento,
                    rutaFotoTemporal,
                    coords.Latitud,
                    coords.Longitud
                );
            }

            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // 3. Agregar relación con el miembro seleccionado
            int parentescoSeleccionado = ParentescoComboBox.SelectedIndex;
            MiembroFamilia seleccionado = (MiembroFamilia)FamiliaresList.SelectedItem;

            ArbolGenealogicoWPF.ArbolGenealogicoService.AgregarNodo(this, seleccionado, f, parentescoSeleccionado);

            // Acá también se actualiza la interfaz gráfica según la implementación elegida
            // Pero sigue pendiente
            // Pero es como tal el dibujo del árbol

            LimpiarFormulario();
        }

        // Eliminar familiar seleccionado
        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (FamiliaresList.SelectedItem is MiembroFamilia sel)
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
            ParentescoComboBox.SelectedIndex = -1; 
            ParentescoComboBox.Text = string.Empty;
            MuerteCheckBox.IsChecked = false;
        }

        private bool VerificarDatos(int modo)
        {
            // Nombre obligatorio
            if (string.IsNullOrWhiteSpace(NombreInput.Text))
            {
                return MostrarError("Por favor ingrese un nombre.");
            }

            // Cédula obligatoria
            if (string.IsNullOrWhiteSpace(CedulaInput.Text))
            {
                return MostrarError("Por favor ingrese una cédula.");
            }

            // Normalizar: quitar espacios, guiones, comas y puntos
            string cedulaNormalizada = CedulaInput.Text.Replace(" ", "").Replace("-", "").Replace(",", "").Replace(".", "");

            // Intentar convertir a entero
            if (!int.TryParse(cedulaNormalizada, out int cedulaNumerica))
            {
                return MostrarError("La cédula debe contener solo números.");
            }

            // Guardar valor ya convertido para asignarlo
            cedulaValida = cedulaNumerica;

            // Verificar duplicados usando la cédula
            if (indicePorCedula.ContainsKey(cedulaValida))
            {
                return MostrarError("Ya existe un miembro con esa cédula.");
            }

            // Fecha obligatoria y válida
            if (string.IsNullOrWhiteSpace(FechaInput.Text) || !DateTime.TryParse(FechaInput.Text, out DateTime fechaValidada))
            {
                return MostrarError("Por favor ingrese una fecha válida.");
            }

            // No permitir fechas futuras
            if (fechaValidada > DateTime.Now)
            {
                return MostrarError("La fecha de nacimiento no puede ser en el futuro.");
            }

            fechaNacimiento = fechaValidada;

            // Coordenadas obligatorias (luego se puede hacer una verificación para ver si son válidas en el sistema)
            if (string.IsNullOrWhiteSpace(CoordenadasInput.Text))
            {
                return MostrarError("Por favor ingrese las coordenadas.");
            }

            // Validar edad solo si el CheckBox está marcado
            if (!estaVivo)
            {
                if (string.IsNullOrWhiteSpace(EdadInput.Text) ||
                    !int.TryParse(EdadInput.Text, out int edad) ||
                    edad <= 0)
                {
                    return MostrarError("Por favor ingrese una edad válida.");
                }

                edadValida = edad;
            }

            // Validar que haya un parentesco seleccionado si se desea agregar al seleccionado
            if (modo == 2)
            {
                if (ParentescoComboBox.SelectedIndex == -1)
                {
                    return MostrarError("Por favor seleccione un parentesco.");
                }
            }
            
            // Foto obligatoria
            if (rutaFotoTemporal == null || FotoPreview.Source == null)
            {
                return MostrarError("Por favor ingrese una foto de la persona.");
            }

            return true;
        }

        private bool MostrarError(string mensaje)
        {
            var errorWin = new ErroresWindow(mensaje);
            errorWin.Owner = this;
            errorWin.ShowDialog();
            return false;
        }

        // Sujeto a cambios según la lógica del otro grafo de distancias
        private (double Latitud, double Longitud) ParsearCoordenadas(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (0, 0);

            var partes = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2)
                return (0, 0);

            if (double.TryParse(partes[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(partes[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            {
                return (lat, lon);
            }

            return (0, 0);
        }
    }
}