using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace ArbolGenealogicoWPF
{
    /// <summary>
    /// Clase de servicio que maneja el árbol genealógico
    /// Solo interesan los métodos de esta clase para manipular el árbol
    /// </summary>
    public static class ArbolGenealogicoService
    {
        // Agregar nodo según relación (ComboBox)
        public static bool AgregarNodo(Window owner, MiembroFamilia seleccionado, MiembroFamilia nuevo, int relacion)
        {
            if (nuevo == null)
                return false;

            try
            {
                switch (relacion)
                {
                    case 0: // Padre
                        seleccionado.AsignarPadre(nuevo);
                        break;

                    case 1: // Madre
                        seleccionado.AsignarMadre(nuevo);
                        break;

                    case 2: // Hijo de padre
                        seleccionado.AsignarHijoComoPadre(nuevo);
                        break;

                    case 3: // Hijo de madre
                        seleccionado.AsignarHijoComoMadre(nuevo);
                        break;

                    case 4: // Pareja
                        seleccionado.AsignarPareja(nuevo);
                        break;

                    case 5: // Hermano
                        seleccionado.AsignarHermano(nuevo);
                        break;
                }

                Debug.WriteLine("===== NUEVA ITERACIÓN EN CURSO =====");
                LigarTodo(seleccionado);
                Debug.WriteLine($"Miembro: {seleccionado.Nombre}");
                Debug.WriteLine($"Padre: {seleccionado.Padre?.Nombre}");
                Debug.WriteLine($"Madre: {seleccionado.Madre?.Nombre}");
                Debug.WriteLine($"Pareja: {seleccionado.Pareja?.Nombre}");
                Debug.WriteLine("Hermanos: " + string.Join(", ", seleccionado.Hermanos.Select(h => h.Nombre)));
                Debug.WriteLine("Hijos: " + string.Join(", ", seleccionado.Hijos.Select(h => h.Nombre)));

                LigarTodo(nuevo);
                Debug.WriteLine($"Miembro: {nuevo.Nombre}");
                Debug.WriteLine($"Padre: {nuevo.Padre?.Nombre}");
                Debug.WriteLine($"Madre: {nuevo.Madre?.Nombre}");
                Debug.WriteLine($"Pareja: {nuevo.Pareja?.Nombre}");
                Debug.WriteLine("Hermanos: " + string.Join(", ", nuevo.Hermanos.Select(h => h.Nombre)));
                Debug.WriteLine("Hijos: " + string.Join(", ", nuevo.Hijos.Select(h => h.Nombre)));


                return true; // Todo salió bien
            }
            catch (InvalidOperationException ex)
            {
                var errorWin = new ErroresWindow(ex.Message) { Owner = owner };
                errorWin.ShowDialog();
                return false;
            }
        }

        public static void LigarTodo(MiembroFamilia miembro)
        {
            if (miembro.Padre != null && miembro.Madre != null)
                miembro.LigarPareja(miembro.Madre, miembro.Padre);

            miembro.LigarHermanos();

            miembro.LigarHermanosConPadres();

            miembro.LigarHijosAPareja();

            if (miembro.Padre != null && miembro.Madre != null)
            {
                miembro.LigarHijosConPadre(miembro.Madre, miembro.Padre);
                miembro.LigarHijosConMadre(miembro.Madre, miembro.Padre);
            }
        }

        public static int[,] GenerarMatriz(IEnumerable<MiembroFamilia> miembros)
        {
            // Convertimos a lista para poder usar Count y acceder por índice
            var lista = miembros.ToList();
            int n = lista.Count;
            int[,] matriz = new int[n, n];

            // Inicializar en -1
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                    matriz[x, y] = -1;

            // índice rápido por cédula
            var index = new Dictionary<int, int>();
            for (int i = 0; i < n; i++)
                index[lista[i].Cedula] = i;

            foreach (var miembro in lista)
            {
                int i = index[miembro.Cedula];

                // Padre ↔ hijo
                if (miembro.Padre != null && index.ContainsKey(miembro.Padre.Cedula))
                {
                    int j = index[miembro.Padre.Cedula];
                    matriz[j, i] = 0; // padre → hijo
                    matriz[i, j] = 2; // hijo → padre
                }

                // Madre ↔ hijo
                if (miembro.Madre != null && index.ContainsKey(miembro.Madre.Cedula))
                {
                    int j = index[miembro.Madre.Cedula];
                    matriz[j, i] = 1; // madre → hijo
                    matriz[i, j] = 3; // hijo → madre
                }

                // Pareja
                if (miembro.Pareja != null && index.ContainsKey(miembro.Pareja.Cedula))
                {
                    int j = index[miembro.Pareja.Cedula];
                    matriz[i, j] = 4;
                    matriz[j, i] = 4;
                }

                // Hermanos
                foreach (var hermano in miembro.Hermanos)
                {
                    if (index.ContainsKey(hermano.Cedula))
                    {
                        int j = index[hermano.Cedula];
                        matriz[i, j] = 5;
                        matriz[j, i] = 5;
                    }
                }
            }

            return matriz;
        }

        //
        public static Dictionary<int, GridCoord> CalcularLayoutCompleto(IEnumerable<MiembroFamilia> miembros)
        {
            // Convertimos a lista para poder usar Count y acceder por índice
            var lista = miembros.ToList();
            int n = lista.Count;

            // Permite traducir relaciones a índices de la matriz
            var index = lista.Select((m, i) => (m, i)).ToDictionary(t => t.m.Cedula, t => t.i);
            var matriz = GenerarMatriz(lista);

            // Diccionario resultante, Row y Col por cédula
            var coords = new Dictionary<int, GridCoord>(n);

            // 1) Asignar filas (Row) por generaciones con DFS
            foreach (var m in lista)
                coords[m.Cedula] = new GridCoord { Row = 0, Col = 0 };

            // Cola de relajación
            var queue = new Queue<int>(Enumerable.Range(0, n));
            var inQueue = new bool[n];
            for (int i = 0; i < n; i++) inQueue[i] = true;

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                inQueue[i] = false;

                var mi = lista[i];
                int rowI = coords[mi.Cedula].Row;

                for (int j = 0; j < n; j++)
                {
                    int w = matriz[i, j];
                    if (w == -1) continue;

                    var mj = lista[j];
                    int rowJ = coords[mj.Cedula].Row;
                    bool changed = false;

                    switch (w)
                    {
                        case 0: // padre → hijo: hijo >= padre + 1
                        case 1: // madre → hijo
                            if (rowJ < rowI + 1)
                            {
                                coords[mj.Cedula] = new GridCoord { Row = rowI + 1, Col = coords[mj.Cedula].Col };
                                changed = true;
                            }
                            break;

                        case 2: // hijo → padre: padre <= hijo - 1
                        case 3: // hijo → madre
                            if (rowJ > rowI - 1)
                            {
                                coords[mj.Cedula] = new GridCoord { Row = rowI - 1, Col = coords[mj.Cedula].Col };
                                changed = true;
                            }
                            break;

                        case 4: // pareja → misma fila
                        case 5: // hermano → misma fila
                            if (rowJ != rowI)
                            {
                                coords[mj.Cedula] = new GridCoord { Row = rowI, Col = coords[mj.Cedula].Col };
                                changed = true;
                            }
                            break;
                    }

                    if (changed && !inQueue[j])
                    {
                        queue.Enqueue(j);
                        inQueue[j] = true;
                    }
                }
            }

            // Normalizar filas (evitar negativos)
            if (coords.Count > 0)
            {
                int minRow = coords.Values.Min(c => c.Row);
                if (minRow < 0)
                {
                    foreach (var k in coords.Keys.ToList())
                        coords[k] = new GridCoord { Row = coords[k].Row - minRow, Col = coords[k].Col };
                }
            }

            // 2) Agrupar por filas y distribuir columnas
            var porFila = coords
                .GroupBy(kvp => kvp.Value.Row)
                .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

            var filaAsignacion = new Dictionary<int, int>();

            int GetNextCol(int row)
            {
                if (!filaAsignacion.ContainsKey(row)) filaAsignacion[row] = 0;
                return filaAsignacion[row]++;
            }

            foreach (var kv in porFila)
            {
                int row = kv.Key;
                var cedulas = kv.Value;

                var gruposHermanos = AgruparPorRelacion(cedulas, index, matriz, 5);

                int colCursor = 0;
                foreach (var grupo in gruposHermanos)
                {
                    var ordenGrupo = OrdenarConParejas(grupo, lista, index, matriz);

                    // Ajuste: si hay pareja, colócalos contiguos
                    for (int k = 0; k < ordenGrupo.Count; k++)
                    {
                        var ced = ordenGrupo[k];
                        var miembro = lista[index[ced]];

                        if (miembro.Pareja != null && ordenGrupo.Contains(miembro.Pareja.Cedula))
                        {
                            // Colocar pareja como bloque
                            coords[ced] = new GridCoord { Row = row, Col = colCursor };
                            coords[miembro.Pareja.Cedula] = new GridCoord { Row = row, Col = colCursor + 1 };
                            colCursor += 2;
                            // Saltar al siguiente después de la pareja
                            k++;
                        }
                        else
                        {
                            coords[ced] = new GridCoord { Row = row, Col = colCursor };
                            colCursor++;
                        }
                    }
                }

                var miembrosFuera = cedulas.Except(gruposHermanos.SelectMany(g => g)).ToList();
                foreach (var ced in miembrosFuera)
                {
                    coords[ced] = new GridCoord { Row = row, Col = colCursor };
                    colCursor++;
                }
            }

            // 3) Centrar ascendientes respecto a descendientes
            for (int iter = 0; iter < 3; iter++)
            {
                foreach (var m in lista)
                {
                    if (m.Hijos.Count == 0) continue;
                    var hijosConCoord = m.Hijos.Where(h => coords.ContainsKey(h.Cedula)).ToList();
                    if (hijosConCoord.Count == 0) continue;

                    int avgCol = (int)Math.Round(hijosConCoord.Average(h => coords[h.Cedula].Col));
                    var cm = coords[m.Cedula];
                    coords[m.Cedula] = new GridCoord { Row = cm.Row, Col = avgCol };

                    if (m.Pareja != null && coords.ContainsKey(m.Pareja.Cedula))
                    {
                        var cp = coords[m.Pareja.Cedula];
                        coords[m.Pareja.Cedula] = new GridCoord { Row = cp.Row, Col = avgCol + 1 };
                    }
                }
            }

            // 4) Resolver solapamientos
            foreach (var kv in porFila)
            {
                int row = kv.Key;
                var cedulas = kv.Value.OrderBy(ced => coords[ced].Col).ToList();

                int lastCol = int.MinValue;
                foreach (var ced in cedulas)
                {
                    var c = coords[ced];
                    int col = c.Col;
                    if (col <= lastCol)
                        col = lastCol + 1;

                    coords[ced] = new GridCoord { Row = row, Col = col };
                    lastCol = col;
                }
            }

            return coords;
        }

        /// <summary>
        /// Construye grupos conectados por un tipo de relación dentro de una fila (e.g., hermanos = 5).
        /// </summary>
        private static List<List<int>> AgruparPorRelacion(
            List<int> cedulasFila,
            Dictionary<int, int> index,
            int[,] matriz,
            int relacionPeso)
        {
            var grupos = new List<List<int>>();
            var pendiente = new HashSet<int>(cedulasFila);

            while (pendiente.Count > 0)
            {
                int seed = pendiente.First();
                pendiente.Remove(seed);

                var grupo = new List<int> { seed };
                var cola = new Queue<int>();
                cola.Enqueue(seed);

                while (cola.Count > 0)
                {
                    int actual = cola.Dequeue();
                    int ia = index[actual];
                    // explorar vecinos por la relación dada
                    foreach (var vecino in cedulasFila)
                    {
                        if (!pendiente.Contains(vecino)) continue;
                        int iv = index[vecino];
                        if (matriz[ia, iv] == relacionPeso)
                        {
                            pendiente.Remove(vecino);
                            grupo.Add(vecino);
                            cola.Enqueue(vecino);
                        }
                    }
                }

                grupos.Add(grupo);
            }

            return grupos;
        }

        /// <summary>
        /// Dentro de un grupo, coloca parejas contiguas y resto alrededor.
        /// </summary>
        private static List<int> OrdenarConParejas(
            List<int> grupo,
            List<MiembroFamilia> miembros,
            Dictionary<int, int> index,
            int[,] matriz)
        {
            // construir pares
            var parejas = new HashSet<int>();
            var orden = new List<int>();

            foreach (var ced in grupo)
            {
                int i = index[ced];
                // buscar pareja dentro del grupo
                foreach (var otro in grupo)
                {
                    if (otro == ced) continue;
                    int j = index[otro];
                    if (matriz[i, j] == 4)
                    {
                        // si ya registramos alguno, saltar
                        if (parejas.Contains(ced) || parejas.Contains(otro))
                            continue;

                        parejas.Add(ced);
                        parejas.Add(otro);
                        orden.Add(ced);
                        orden.Add(otro);
                        break;
                    }
                }
            }

            // agregar los que no tienen pareja al final
            foreach (var ced in grupo)
            {
                if (!parejas.Contains(ced))
                    orden.Add(ced);
            }

            return orden;
        }
    }
}