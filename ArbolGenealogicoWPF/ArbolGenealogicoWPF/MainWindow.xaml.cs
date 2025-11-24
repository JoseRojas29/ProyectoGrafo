using System;
using System.Collections.Generic;
using System.Linq;
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
                    CoordenadasInput.Text.Trim()
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
                    CoordenadasInput.Text.Trim()
                );
            }

            // 5. Guardar en las estructuras
            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // 6. Recalcular layout completo, redibujar y limpiar formulario
            RedibujarArbol(ArbolCanvas, familiares);
            LimpiarFormulario();
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Verificar validez de datos del primer miembro
            if (!VerificarDatos(2))
                return;

            // 2. Crear el nuevo miembro
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
                    CoordenadasInput.Text.Trim()
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
                    CoordenadasInput.Text.Trim()
                );
            }

            // 3. Obtener seleccionado y parentesco
            int parentescoSeleccionado = ParentescoComboBox.SelectedIndex;
            MiembroFamilia seleccionado = (MiembroFamilia)FamiliaresList.SelectedItem;

            // 4. Intentar agregar la relación, si no es posible, salir
            bool exito = ArbolGenealogicoService.AgregarNodo(this, seleccionado, f, parentescoSeleccionado);

            if (!exito)
            {
                return;
            }

            // 5. Guardar en las estructuras
            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;
            
            // 6. Redibujar el árbol y limpiar el formulario
            RedibujarArbol(ArbolCanvas, familiares);
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

            // Coordenadas obligatorias
            if (string.IsNullOrWhiteSpace(CoordenadasInput.Text))
            {
                return MostrarError("Por favor ingrese las coordenadas.");
            }

            // Validar formato de coordenadas
            var partes = CoordenadasInput.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2)
            {
                return MostrarError("Las coordenadas deben tener el formato 'latitud,longitud'.");
            }

            var style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;

            if (!double.TryParse(partes[0].Trim(), style, culture, out double latitud))
            {
                return MostrarError("La latitud no es válida.");
            }

            if (!double.TryParse(partes[1].Trim(), style, culture, out double longitud))
            {
                return MostrarError("La longitud no es válida.");
            }

            // Verificar rango permitido
            if (latitud < 8.0 || latitud > 11.5)
            {
                return MostrarError($"La latitud debe estar entre 8.0 y 11.5.");
            }

            if (longitud < -86.0 || longitud > -82.0)
            {
                return MostrarError($"La longitud debe estar entre -86.0 y -82.0.");
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
            var color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#B833FF"));

            // 1. Calcular coordenadas y matriz
            var coords = ArbolGenealogicoService.CalcularLayoutCompleto(lista);
            var index = lista.ToDictionary(m => m.Cedula, m => lista.IndexOf(m));
            var matriz = ArbolGenealogicoService.GenerarMatriz(lista);

            // 2. Dibujar nodos
            visualPorCedula = new Dictionary<int, Border>();
            foreach (var m in lista)
            {
                var c = coords[m.Cedula];
                var p = GridToPixel(c);

                var grid = new Grid
                {
                    Width = 120,
                    Height = 110
                };

                var nombreText = new TextBlock
                {
                    Text = m.Nombre,
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center,
                    Foreground = color,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.None,
                    MaxWidth = 120,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                grid.Children.Add(nombreText);

                var cont = new Border
                {
                    Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#1A0033")),
                    BorderBrush = color,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Child = grid,
                    Padding = new Thickness(8),
                    Tag = m
                };
                cont.MouseLeftButtonDown += Nodo_Click;

                Canvas.SetLeft(cont, p.X);
                Canvas.SetTop(cont, p.Y);
                arbolCanvas.Children.Add(cont);
                visualPorCedula[m.Cedula] = cont;
            }

            // 3. Dibujar conexiones
            var parejaCentros = new Dictionary<(int, int), Point>();
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

                    DibujarConexion(arbolCanvas, mi, mj, w, pi, pj, parejaCentros);
                }
            }

            // 4. Dibujar línea común para hermanos
            var hermanosPorFila = lista
                .GroupBy(m => coords[m.Cedula].Row)
                .Select(g => g.Where(m => m.Hermanos.Any()).ToList())
                .Where(grupo => grupo.Count > 1);

            foreach (var grupo in hermanosPorFila)
            {
                var puntos = grupo.Select(m => GridToPixel(coords[m.Cedula])).ToList();
                double minX = puntos.Min(p => p.X + 60);
                double maxX = puntos.Max(p => p.X + 60);
                double y = puntos.Min(p => p.Y) - 10;

                var lineaSuperior = new Line
                {
                    X1 = minX,
                    Y1 = y,
                    X2 = maxX,
                    Y2 = y,
                    Stroke = color,
                    StrokeThickness = 2
                };
                arbolCanvas.Children.Add(lineaSuperior);

                foreach (var p in puntos)
                {
                    var vertical = new Line
                    {
                        X1 = p.X + 60,
                        Y1 = y,
                        X2 = p.X + 60,
                        Y2 = p.Y,
                        Stroke = color,
                        StrokeThickness = 2
                    };
                    arbolCanvas.Children.Add(vertical);
                }
            }
        }

        private void DibujarConexion(Canvas canvas, MiembroFamilia mi, MiembroFamilia mj, int tipo, Point pi, Point pj, Dictionary<(int, int), Point> parejaCentros)
        {
            var color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#B833FF"));

            switch (tipo)
            {
                case 4: // pareja
                    bool parejaALaDerecha = pj.X > pi.X;

                    double x1 = parejaALaDerecha ? pi.X + 137 : pi.X; // borde lateral del nodo izquierdo
                    double x2 = parejaALaDerecha ? pj.X : pj.X + 137; // borde lateral del nodo derecho
                    double y = pi.Y + 55; // altura media del nodo

                    var parejaLine = new Line
                    {
                        X1 = x1,
                        Y1 = y,
                        X2 = x2,
                        Y2 = y,
                        Stroke = color,
                        StrokeThickness = 2
                    };
                    canvas.Children.Add(parejaLine);

                    var key = (Math.Min(mi.Cedula, mj.Cedula), Math.Max(mi.Cedula, mj.Cedula));
                    parejaCentros[key] = new Point((x1 + x2) / 2.0, y);
                    break;

                case 0: // padre → hijo
                case 1: // madre → hijo
                    var hijoX = pj.X + 60;
                    var hijoY = pj.Y;

                    Point origen;
                    var keyPareja = mi.Pareja != null
                        ? (Math.Min(mi.Cedula, mi.Pareja.Cedula), Math.Max(mi.Cedula, mi.Pareja.Cedula))
                        : default;

                    bool usarCentroDePareja = mi.Pareja != null && parejaCentros.ContainsKey(keyPareja);

                    if (usarCentroDePareja)
                    {
                        origen = parejaCentros[keyPareja];
                    }
                    else
                    {
                        // Padre sin pareja: simular línea horizontal desde borde derecho hacia punto virtual
                        double bordeDerechoX = pi.X + 137;
                        double centroX = bordeDerechoX + 20;
                        double centroY = pi.Y + 55;

                        var lineaHorizontal = new Line
                        {
                            X1 = bordeDerechoX,
                            Y1 = centroY,
                            X2 = centroX,
                            Y2 = centroY,
                            Stroke = color,
                            StrokeThickness = 2
                        };
                        canvas.Children.Add(lineaHorizontal);

                        origen = new Point(centroX, centroY);
                    }

                    double lineaY = hijoY - 10;

                    var polyPadre = new Polyline
                    {
                        Stroke = color,
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(origen.X, origen.Y),
                            new Point(origen.X, lineaY),
                            new Point(hijoX, lineaY),
                            new Point(hijoX, hijoY)
                        }
                    };
                    canvas.Children.Add(polyPadre);
                    break;





                case 2:
                case 3:
                case 5: // Ya contemplados
                    break;
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