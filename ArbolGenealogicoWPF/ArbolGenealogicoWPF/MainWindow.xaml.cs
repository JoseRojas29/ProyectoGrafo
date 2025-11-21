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

        // Diccionario para las coordenadas en el árbol, para saber su posición en pixeles
        private Dictionary<int, GridCoord> cedulaACoord = new();

        // Diccionario para actualizar posición, estilo o eliminar el nodo visual si se borra.
        private Dictionary<int, FrameworkElement> cedulaAVisual = new Dictionary<int, FrameworkElement>();

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
            cedulaACoord.Clear();
            cedulaAVisual.Clear();

            // 3. Limpiar interfaz gráfica
            ArbolCanvas.Children.Clear();

            // 4. Crear el nuevo miembro (el primero de todos)
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

            // 5. Guardar en las estructuras
            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // 6. Asignar coordenadas y dibujar 
            AsignarCoordenadasSiFaltan(f);
            DibujarNodo(f);
            DibujarConexiones(f);

            // 7. Limpiar formulario para el siguiente ingreso
            LimpiarFormulario();
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

            // 3. Guardar en las estructuras
            familiares.Add(f);
            indicePorCedula[cedulaValida] = f;

            // 4. Agregar relación con el miembro seleccionado
            int parentescoSeleccionado = ParentescoComboBox.SelectedIndex;
            MiembroFamilia seleccionado = (MiembroFamilia)FamiliaresList.SelectedItem;
            ArbolGenealogicoService.AgregarNodo(this, seleccionado, f, parentescoSeleccionado);

            // 5. Asignar coordenadas y dibujar
            AsignarCoordenadasSiFaltan(f);
            DibujarNodo(f);
            DibujarConexiones(f);

            // 6. Limpiar formulario para el siguiente ingreso
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
        private Point GridToPixel(GridCoord g)
        {
            if (cedulaACoord.Count == 0)
                return new Point(ArbolCanvas.ActualWidth / 2, ArbolCanvas.ActualHeight / 2);

            int minRow = cedulaACoord.Values.Min(c => c.Row);
            int maxRow = cedulaACoord.Values.Max(c => c.Row);
            int minCol = cedulaACoord.Values.Min(c => c.Col);
            int maxCol = cedulaACoord.Values.Max(c => c.Col);

            int centerRow = (minRow + maxRow) / 2;
            int centerCol = (minCol + maxCol) / 2;

            double x = (ArbolCanvas.ActualWidth / 2) + (g.Col - centerCol) * CellWidth;
            double y = (ArbolCanvas.ActualHeight / 2) + (g.Row - centerRow) * RowHeight;

            return new Point(x, y);
        }

        private void DibujarNodo(MiembroFamilia miembro)
        {
            // Si ya existe el visual, lo actualizamos en vez de crear uno nuevo
            if (cedulaAVisual.ContainsKey(miembro.Cedula))
            {
                var panelExistente = cedulaAVisual[miembro.Cedula];
                var coordExistente = cedulaACoord[miembro.Cedula];
                var puntoExistente = GridToPixel(coordExistente);

                Canvas.SetLeft(panelExistente, puntoExistente.X);
                Canvas.SetTop(panelExistente, puntoExistente.Y);
                return;
            }

            // Crear visual del nodo
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 100,
                Height = 120,
                Tag = miembro
            };

            var foto = new Image
            {
                Source = new BitmapImage(new Uri(miembro.FotografiaRuta, UriKind.RelativeOrAbsolute)),
                Width = 80,
                Height = 80
            };

            var nombre = new TextBlock
            {
                Text = miembro.Nombre,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(foto);
            panel.Children.Add(nombre);

            panel.MouseLeftButtonDown += Nodo_Click;

            // Obtener coordenadas lógicas y convertirlas a píxeles
            var coord = cedulaACoord[miembro.Cedula];
            var punto = GridToPixel(coord);

            Canvas.SetLeft(panel, punto.X);
            Canvas.SetTop(panel, punto.Y);

            // Agregar al Canvas y guardar referencia
            ArbolCanvas.Children.Add(panel);
            cedulaAVisual[miembro.Cedula] = panel;
        }

        private void DibujarConexiones(MiembroFamilia miembro)
        {
            var origenCoord = cedulaACoord[miembro.Cedula];
            var origenPixel = GridToPixel(origenCoord);

            // Padre ↔ hijo
            if (miembro.Padre != null && cedulaACoord.ContainsKey(miembro.Padre.Cedula))
                DibujarLineaPadreHijo(miembro.Padre.Cedula, miembro.Cedula);

            if (miembro.Madre != null && cedulaACoord.ContainsKey(miembro.Madre.Cedula))
                DibujarLineaPadreHijo(miembro.Madre.Cedula, miembro.Cedula);

            // Pareja ↔ pareja
            if (miembro.Pareja != null && cedulaACoord.ContainsKey(miembro.Pareja.Cedula))
                DibujarLineaPareja(miembro.Cedula, miembro.Pareja.Cedula);

            // Hermanos ↔ hermanos
            foreach (var hermano in miembro.Hermanos)
            {
                if (cedulaACoord.ContainsKey(hermano.Cedula))
                    DibujarLineaHermano(miembro.Cedula, hermano.Cedula);
            }
        }

        private void DibujarLineaPadreHijo(int padreCed, int hijoCed)
        {
            var padrePixel = GridToPixel(cedulaACoord[padreCed]);
            var hijoPixel = GridToPixel(cedulaACoord[hijoCed]);

            double parentX = padrePixel.X + 50; // centro del nodo padre
            double parentY = padrePixel.Y + 120; // parte inferior del nodo padre
            double childX = hijoPixel.X + 50;   // centro del nodo hijo
            double childY = hijoPixel.Y;        // parte superior del nodo hijo
            double midY = (parentY + childY) / 2;

            var points = new PointCollection
            {
                new Point(parentX, parentY),
                new Point(parentX, midY),
                new Point(childX, midY),
                new Point(childX, childY)
            };

            var poly = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Points = points
            };

            ArbolCanvas.Children.Add(poly);
        }

        private void DibujarLineaPareja(int cedA, int cedB)
        {
            var aPixel = GridToPixel(cedulaACoord[cedA]);
            var bPixel = GridToPixel(cedulaACoord[cedB]);

            double ax = aPixel.X + 50;
            double ay = aPixel.Y + 60;
            double bx = bPixel.X + 50;
            double by = bPixel.Y + 60;

            var line = new Line
            {
                X1 = ax,
                Y1 = ay,
                X2 = bx,
                Y2 = by,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            ArbolCanvas.Children.Add(line);
        }

        private void DibujarLineaHermano(int cedA, int cedB)
        {
            var aPixel = GridToPixel(cedulaACoord[cedA]);
            var bPixel = GridToPixel(cedulaACoord[cedB]);

            double ax = aPixel.X + 50;
            double ay = aPixel.Y;
            double bx = bPixel.X + 50;
            double by = bPixel.Y;

            var line = new Line
            {
                X1 = ax,
                Y1 = ay,
                X2 = bx,
                Y2 = by,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            ArbolCanvas.Children.Add(line);
        }

        private void AsignarCoordenadasSiFaltan(MiembroFamilia miembro)
        {
            // Si ya tiene coordenadas, no hacemos nada
            if (cedulaACoord.ContainsKey(miembro.Cedula))
                return;

            int row = 0;
            int col = 0;

            // Caso 1: tiene padre o madre → fila = padre.Row + 1
            if (miembro.Padre != null && cedulaACoord.ContainsKey(miembro.Padre.Cedula))
            {
                row = cedulaACoord[miembro.Padre.Cedula].Row + 1;
                col = cedulaACoord[miembro.Padre.Cedula].Col;
            }
            else if (miembro.Madre != null && cedulaACoord.ContainsKey(miembro.Madre.Cedula))
            {
                row = cedulaACoord[miembro.Madre.Cedula].Row + 1;
                col = cedulaACoord[miembro.Madre.Cedula].Col;
            }

            // Caso 2: tiene hijos → fila = hijo.Row - 1
            else if (miembro.Hijos.Any(h => cedulaACoord.ContainsKey(h.Cedula)))
            {
                var hijo = miembro.Hijos.First(h => cedulaACoord.ContainsKey(h.Cedula));
                row = cedulaACoord[hijo.Cedula].Row - 1;
                col = cedulaACoord[hijo.Cedula].Col;
            }

            // Caso 3: tiene hermanos → misma fila, columna contigua
            else if (miembro.Hermanos.Any(h => cedulaACoord.ContainsKey(h.Cedula)))
            {
                var hermano = miembro.Hermanos.First(h => cedulaACoord.ContainsKey(h.Cedula));
                row = cedulaACoord[hermano.Cedula].Row;
                col = cedulaACoord[hermano.Cedula].Col + 1;
            }

            // Caso 4: primer nodo → origen
            else
            {
                row = 0;
                col = 0;
            }

            // Guardar coordenadas
            cedulaACoord[miembro.Cedula] = new GridCoord { Row = row, Col = col };
        }

        private void Nodo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not StackPanel panel)
                return; // si no es un StackPanel, no hacemos nada

            if (panel.Tag is not MiembroFamilia miembro)
                return; // si el Tag no es un MiembroFamilia, no hacemos nada

            // Guardar como seleccionado
            FamiliaresList.SelectedItem = miembro;

            // Opcional: resaltar visualmente el nodo
            panel.Background = Brushes.LightBlue;
        }
    }
}