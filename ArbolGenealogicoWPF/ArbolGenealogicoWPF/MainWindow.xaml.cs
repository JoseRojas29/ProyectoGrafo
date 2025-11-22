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
using System.Security;

namespace ArbolGenealogicoWPF
{
    public partial class MainWindow : WindowBaseLogica
    {
        // Colección principal de familiares, lista con todos los nodos hasta el momento
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

        // Diccionario para actualizar posición, estilo o eliminar el nodo visual si se borra.
        private Dictionary<int, Border> visualPorCedula = new();

        // Espaciado entre nodos (Se ajusta según como vea los tamaños ya al correr)
        private const int CellWidth = 180;   // ancho de cada celda lógica
        private const int RowHeight = 160;   // alto de cada fila lógica

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

        private void Deseleccionar_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar selección en la lista
            FamiliaresList.SelectedItem = null;

            // Quitar resaltado de todos los nodos
            foreach (var b in visualPorCedula.Values)
                b.Background = Brushes.Transparent;
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
            visualPorCedula.Clear();

            // 3. Limpiar interfaz gráfica
            ArbolCanvas.Children.Clear();

            // 4. Crear el nuevo miembro (el primero de todos)
            var coordsAux = ParsearCoordenadas(CoordenadasInput.Text.Trim());

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
                    coordsAux.Latitud,
                    coordsAux.Longitud
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
                    coordsAux.Latitud,
                    coordsAux.Longitud
                );
            }

            // 5. Guardar en las estructuras
            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // 5. Recalcular layout completo y redibujar
            var coords = ArbolGenealogicoService.CalcularLayoutCompleto(familiares);
            RedibujarArbol(ArbolCanvas, familiares);

            // 7. Limpiar formulario para el siguiente ingreso
            LimpiarFormulario();
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Verificar validez de datos del primer miembro
            if (!VerificarDatos(2))
                return;

            // 2. Crear el nuevo miembro
            var coordsAux = ParsearCoordenadas(CoordenadasInput.Text.Trim());

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
                    coordsAux.Latitud,
                    coordsAux.Longitud
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
                    coordsAux.Latitud,
                    coordsAux.Longitud
                );
            }

            // 3. Guardar en las estructuras
            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // 4. Agregar relación con el miembro seleccionado
            int parentescoSeleccionado = ParentescoComboBox.SelectedIndex;
            MiembroFamilia seleccionado = (MiembroFamilia)FamiliaresList.SelectedItem;
            ArbolGenealogicoService.AgregarNodo(this, seleccionado, f, parentescoSeleccionado);

            // 5. Recalcular layout completo y redibujar
            RedibujarArbol(ArbolCanvas, familiares);

            // 6. Limpiar formulario para el siguiente ingreso
            LimpiarFormulario();
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (FamiliaresList.SelectedItem is not MiembroFamilia sel)
            {
                var errorWin = new ErroresWindow("Por favor seleccione un familiar.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return;
            }

            // Verificar si es nodo puente
            if (EsNodoPuente(sel, familiares))
            {
                var errorWin = new ErroresWindow($"No se puede eliminar a {sel.Nombre}.\nEsto fragmentaría al árbol actual en dos.");
                errorWin.Owner = this;
                errorWin.ShowDialog();
                return;
            }

            // 1. Limpiar relaciones inversas
            if (sel.Padre != null)
                sel.Padre.Hijos.Remove(sel);

            if (sel.Madre != null)
                sel.Madre.Hijos.Remove(sel);

            sel.QuitarParejaEnElOtroLado();

            foreach (var hijo in sel.Hijos.ToList())
            {
                if (hijo.Padre == sel) hijo.QuitarPadre();
                if (hijo.Madre == sel) hijo.QuitarMadre();
            }

            foreach (var hermano in sel.Hermanos.ToList())
                hermano.Hermanos.Remove(sel);

            // 2. Eliminar de los dos diccionarios y del canvas
            if (visualPorCedula.TryGetValue(sel.Cedula, out var border))
            {
                var parentCanvas = VisualTreeHelper.GetParent(border) as Canvas;
                parentCanvas?.Children.Remove(border);
                visualPorCedula.Remove(sel.Cedula);
            }

            indicePorCedula.Remove(sel.Cedula);

            // 3. Eliminar de la lista global
            familiares.Remove(sel);

            // 4. Redibujar el árbol para limpiar líneas
            RedibujarArbol(ArbolCanvas, familiares);
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
            FamiliaresList.SelectedItem = null;

            foreach (var panel in visualPorCedula.Values)
                panel.Background = Brushes.Transparent;
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

                if (FamiliaresList.SelectedItem == null)
                {
                    return MostrarError("Por favor seleccione un familiar.");
                }

                MiembroFamilia seleccionado = (MiembroFamilia)FamiliaresList.SelectedItem;

                // Validar lógica de fechas entre padre/madre e hijo
                if (ParentescoComboBox.SelectedIndex == 0 || ParentescoComboBox.SelectedIndex == 1) // Padre o madre
                {
                    if (seleccionado.FechaNacimiento <= fechaNacimiento)
                    {
                        return MostrarError("El ascendiente debe tener una fecha de nacimiento anterior.");
                    }
                }
                else if (ParentescoComboBox.SelectedIndex == 2 || ParentescoComboBox.SelectedIndex == 3) // Hija o hijo
                {
                    if (seleccionado.FechaNacimiento >= fechaNacimiento)
                    {
                        return MostrarError("El descendiente debe tener una fecha de nacimiento posterior.");
                    }
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

        private bool EsNodoPuente(MiembroFamilia miembro, IEnumerable<MiembroFamilia> familiares)
        {
            // Si el miembro no existe o no hay nadie más, no es puente
            if (miembro == null || familiares == null) return false;

            // 1) Obtener los vecinos directos del miembro (su "grado" en el grafo)
            var vecinosMiembro = ObtenerVecinosDistintos(miembro);

            // Corte rápido: grado 0 o 1 => no puede desconectar nada
            if (vecinosMiembro.Count <= 1)
                return false;

            // 2) Hallar el componente conectado de 'miembro' (con el miembro presente)
            var componente = new HashSet<MiembroFamilia>();
            var stackComp = new Stack<MiembroFamilia>();
            stackComp.Push(miembro);

            while (stackComp.Count > 0)
            {
                var actual = stackComp.Pop();
                if (!componente.Add(actual)) continue;

                foreach (var v in ObtenerVecinosDistintos(actual))
                    if (!componente.Contains(v))
                        stackComp.Push(v);
            }

            // Si el componente tiene solo al miembro, no hay nada que desconectar
            if (componente.Count == 1)
                return false;

            // 3) Simular la eliminación: iniciar DFS desde cualquier vecino del miembro
            // pero sin poder pasar por 'miembro'. La meta es visitar el resto del componente.
            var origen = vecinosMiembro.First(); // existe porque grado >= 2
            var visitados = new HashSet<MiembroFamilia>();
            var stack = new Stack<MiembroFamilia>();
            stack.Push(origen);

            while (stack.Count > 0)
            {
                var actual = stack.Pop();
                if (!visitados.Add(actual)) continue;

                foreach (var v in ObtenerVecinosDistintos(actual))
                {
                    if (v == miembro) continue;          // "removemos" el nodo
                    if (!visitados.Contains(v))
                        stack.Push(v);
                }
            }

            // 4) Comprobación: ¿visitamos todo el componente salvo al propio miembro?
            // Si no, eliminar 'miembro' desconecta el componente => es puente.
            return visitados.Count < (componente.Count - 1);
        }

        private List<MiembroFamilia> ObtenerVecinosDistintos(MiembroFamilia m)
        {
            var vecinos = new HashSet<MiembroFamilia>();

            if (m.Padre != null) vecinos.Add(m.Padre);
            if (m.Madre != null) vecinos.Add(m.Madre);
            if (m.Pareja != null) vecinos.Add(m.Pareja);

            foreach (var h in m.Hijos)
                if (h != null) vecinos.Add(h);

            foreach (var he in m.Hermanos)
                if (he != null) vecinos.Add(he);

            return vecinos.ToList();
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

        // ============================================
        //         MÉTODOS GRÁFICOS DEL ÁRBOL
        // ============================================
        public static Point GridToPixel(GridCoord c)
        {
            return new Point(c.Col * CellWidth, c.Row * RowHeight);
        }

        public void RedibujarArbol(Canvas arbolCanvas, IEnumerable<MiembroFamilia> miembros)
        {
            var lista = miembros.ToList();
            arbolCanvas.Children.Clear();

            // 1. Capa invisible al fondo (para detectar clics en áreas vacías)
            var fondo = new Rectangle
            {
                Width = arbolCanvas.ActualWidth,
                Height = arbolCanvas.ActualHeight,
                Fill = Brushes.Transparent // invisible pero clickeable
            };
            fondo.MouseLeftButtonDown += ArbolCanvas_MouseLeftButtonDown;
            arbolCanvas.Children.Add(fondo);

            // 2. Calcular coordenadas y matriz
            var coords = ArbolGenealogicoService.CalcularLayoutCompleto(lista);
            var index = lista.ToDictionary(m => m.Cedula, m => lista.IndexOf(m));
            var matriz = ArbolGenealogicoService.GenerarMatriz(lista);

            // 3. Dibujar nodos
            visualPorCedula = new Dictionary<int, Border>();
            foreach (var m in lista)
            {
                var c = coords[m.Cedula];
                var p = GridToPixel(c);

                var panel = new StackPanel { Orientation = Orientation.Vertical, Width = 120 };
                panel.Children.Add(new Image { Width = 80, Height = 80, Stretch = Stretch.UniformToFill });
                panel.Children.Add(new TextBlock { Text = m.Nombre, FontSize = 14, TextAlignment = TextAlignment.Center });

                var cont = new Border
                {
                    Background = Brushes.Transparent, // base transparente
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Child = panel,
                    Padding = new Thickness(8),
                    Tag = m
                };
                cont.MouseLeftButtonDown += Nodo_Click;

                Canvas.SetLeft(cont, p.X);
                Canvas.SetTop(cont, p.Y);
                arbolCanvas.Children.Add(cont);
                visualPorCedula[m.Cedula] = cont;
            }

            // 4. Dibujar conexiones (líneas)
            for (int i = 0; i < lista.Count; i++)
            {
                for (int j = 0; j < lista.Count; j++)
                {
                    int w = matriz[i, j];
                    if (w == -1) continue;

                    var mi = lista[i];
                    var mj = lista[j];
                    var pi = GridToPixel(coords[mi.Cedula]);
                    var pj = GridToPixel(coords[mj.Cedula]);

                    var line = new Line
                    {
                        X1 = pi.X + 60,
                        Y1 = pi.Y + 40,
                        X2 = pj.X + 60,
                        Y2 = pj.Y + 40,
                        StrokeThickness = w is 4 or 5 ? 2 : 3,
                        Stroke = w switch
                        {
                            0 => Brushes.DarkGreen, // padre→hijo
                            1 => Brushes.SeaGreen,  // madre→hijo
                            2 => Brushes.DarkBlue,  // hijo→padre
                            3 => Brushes.SlateBlue, // hijo→madre
                            4 => Brushes.Orange,    // pareja
                            5 => Brushes.Gray,      // hermanos
                            _ => Brushes.Black
                        }
                    };

                    arbolCanvas.Children.Add(line);
                }
            }
        }

        private void Nodo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not MiembroFamilia miembro)
                return;

            // Limpiar resaltado previo
            foreach (var b in visualPorCedula.Values)
                b.Background = Brushes.Transparent;

            // Guardar como seleccionado
            FamiliaresList.SelectedItem = miembro;

            // Resaltar visualmente
            border.Background = Brushes.LightBlue;

            e.Handled = true;
        }

        private void ArbolCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Canvas) // solo si el clic fue en el Canvas vacío
            {
                FamiliaresList.SelectedItem = null;

                foreach (var b in visualPorCedula.Values)
                    b.Background = Brushes.Transparent; // volver al estado base
            }
        }
    }
}