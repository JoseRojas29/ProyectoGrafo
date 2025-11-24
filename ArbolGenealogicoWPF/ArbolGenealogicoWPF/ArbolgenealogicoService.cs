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

        /// <summary>
        /// Calcula el layout completo del árbol genealógico en un canvas lógico.
        /// 
        /// El algoritmo sigue tres fases principales:
        /// 1. Asignar filas (Row) según generación usando propagación de relaciones
        ///    - Padres/madres arriba, hijos abajo, parejas y hermanos en la misma fila.
        /// 2. Distribuir columnas (Col) dentro de cada fila
        ///    - Se ordenan los miembros y se colocan las parejas contiguas.
        ///    - Las columnas se manejan como double para permitir centrado preciso.
        /// 3. Centrar padres respecto a sus hijos (procesando filas en reversa)
        ///    - Los hijos se fijan primero en la última fila.
        ///    - Los padres se colocan en el centro geométrico del bloque de hijos.
        ///    - Las personas sin hijos se encolan y se colocan al final de la fila.
        /// 
        /// Devuelve un diccionario: cédula → coordenadas (Row, Col),
        /// donde Row indica la generación y Col la posición horizontal.
        /// </summary>

        public static Dictionary<int, GridCoord> CalcularLayoutCompleto(IEnumerable<MiembroFamilia> miembros)
        {
            // ==========================================================================================
            //                                 0. Preparación de datos
            // ==========================================================================================

            // Convertimos a lista para poder usar Count y acceder por índice
            var miembrosLista = miembros.ToList();
            int totalMiembros = miembrosLista.Count;

            // Permite traducir cédulas a índices de la matriz de relaciones
            var indicePorCedula = miembrosLista.Select((miembro, indice) => (miembro, indice))
                .ToDictionary(t => t.miembro.Cedula, t => t.indice);

            // Matriz de relaciones entre miembros 
            var matrizRelaciones = GenerarMatriz(miembrosLista);

            // Diccionario resultante: coordenadas por cédula (fila = generación, columna = posición horizontal)
            var coordenadasPorCedula = new Dictionary<int, GridCoord>(totalMiembros);


            // ==========================================================================================
            //                 1. DEFINIR FILAS DE LOS MIEMBROS SEGÚN GENERACIÓN
            // ==========================================================================================

            // Inicializar todas las filas en 0 (misma generación por defecto) 
            foreach (var miembro in miembrosLista)
                coordenadasPorCedula[miembro.Cedula] = new GridCoord { Row = 0, Col = 0.0 };

            // Cola de relajación para propagar ajustes de generación
            var colaRelajacion = new Queue<int>(Enumerable.Range(0, totalMiembros));
            var enCola = new bool[totalMiembros];
            for (int k = 0; k < totalMiembros; k++) enCola[k] = true;

            // Propagación de generación según relaciones (padres arriba, hijos abajo, parejas/hermanos misma fila)
            while (colaRelajacion.Count > 0)
            {
                int indiceActual = colaRelajacion.Dequeue();
                enCola[indiceActual] = false;

                var personaActual = miembrosLista[indiceActual];
                int filaActual = coordenadasPorCedula[personaActual.Cedula].Row;

                for (int indiceVecino = 0; indiceVecino < totalMiembros; indiceVecino++)
                {
                    int relacion = matrizRelaciones[indiceActual, indiceVecino];
                    if (relacion == -1) continue;

                    var personaVecino = miembrosLista[indiceVecino];
                    int filaVecino = coordenadasPorCedula[personaVecino.Cedula].Row;
                    bool seActualizo = false;

                    switch (relacion)
                    {
                        case 0: // padre → hijo: hijo >= padre + 1
                        case 1: // madre → hijo
                            if (filaVecino < filaActual + 1)
                            {
                                coordenadasPorCedula[personaVecino.Cedula] = new GridCoord
                                {
                                    Row = filaActual + 1,
                                    Col = coordenadasPorCedula[personaVecino.Cedula].Col
                                };
                                seActualizo = true;
                            }
                            break;

                        case 2: // hijo → padre: padre <= hijo - 1
                        case 3: // hijo → madre
                            if (filaVecino > filaActual - 1)
                            {
                                coordenadasPorCedula[personaVecino.Cedula] = new GridCoord
                                {
                                    Row = filaActual - 1,
                                    Col = coordenadasPorCedula[personaVecino.Cedula].Col
                                };
                                seActualizo = true;
                            }
                            break;

                        case 4: // pareja → misma fila
                        case 5: // hermano → misma fila
                            if (filaVecino != filaActual)
                            {
                                coordenadasPorCedula[personaVecino.Cedula] = new GridCoord
                                {
                                    Row = filaActual,
                                    Col = coordenadasPorCedula[personaVecino.Cedula].Col
                                };
                                seActualizo = true;
                            }
                            break;
                    }

                    if (seActualizo && !enCola[indiceVecino])
                    {
                        colaRelajacion.Enqueue(indiceVecino);
                        enCola[indiceVecino] = true;
                    }
                }
            }

            // Normalizar filas (evitar valores negativos en la generación)
            if (coordenadasPorCedula.Count > 0)
            {
                // Buscar la fila más baja (mínima generación asignada)
                int filaMinima = coordenadasPorCedula.Values.Min(coord => coord.Row);

                // Si alguien quedó en negativo, subir a todos para que el mínimo sea 0
                if (filaMinima < 0)
                {
                    foreach (var cedula in coordenadasPorCedula.Keys.ToList())
                    {
                        var coordActual = coordenadasPorCedula[cedula];
                        coordenadasPorCedula[cedula] = new GridCoord
                        {
                            Row = coordActual.Row - filaMinima, // desplazar hacia arriba
                            Col = coordActual.Col               // mantener la columna igual
                        };
                    }
                }
            }


            // ==========================================================================================
            //          2. ORDENAR MIEMBROS DENTRO DE CADA FILA (ASEGURAR PAREJAS JUNTAS)
            // ==========================================================================================

            // Agrupar por fila (cada generación) usando las filas definidas en fase 1
            var cedulasPorFila = coordenadasPorCedula
                .GroupBy(kvp => kvp.Value.Row)
                .OrderBy(g => g.Key) // recorrer de arriba hacia abajo
                .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

            foreach (var kv in cedulasPorFila)
            {
                int row = kv.Key;
                var cedulas = kv.Value;

                var gruposHermanos = AgruparPorRelacion(cedulas, indicePorCedula, matrizRelaciones, 5);

                var colocadosFila = new HashSet<int>();
                int colCursor = 0;

                // Helper local: crear bloques dentro de un conjunto de cédulas del grupo
                List<List<int>> CrearBloques(List<int> conjunto)
                {
                    var bloques = new List<List<int>>();
                    var visto = new HashSet<int>();

                    foreach (var ced in conjunto)
                    {
                        if (visto.Contains(ced)) continue;

                        var m = miembrosLista[indicePorCedula[ced]];
                        bool parejaEnFila = m.Pareja != null && cedulas.Contains(m.Pareja.Cedula);

                        if (parejaEnFila && !visto.Contains(m.Pareja.Cedula))
                        {
                            // Bloque de 2: miembro + pareja
                            bloques.Add(new List<int> { ced, m.Pareja.Cedula });
                            visto.Add(ced);
                            visto.Add(m.Pareja.Cedula);
                        }
                        else
                        {
                            // Bloque de 1
                            bloques.Add(new List<int> { ced });
                            visto.Add(ced);
                        }
                    }
                    return bloques;
                }

                int gruposColocados = 0;

                // 1) Procesar grupos de hermanos a nivel de bloques
                foreach (var grupo in gruposHermanos)
                {
                    // Orden sugerida a nivel de individuos (si te aporta)
                    var ordenGrupo = OrdenarConParejas(grupo, miembrosLista, indicePorCedula, matrizRelaciones);

                    // Crear bloques basados en la presencia real de parejas en la fila
                    var bloques = CrearBloques(ordenGrupo);

                    int colocadosAntesDelGrupo = colocadosFila.Count;

                    // Asignar columnas bloque por bloque
                    foreach (var bloque in bloques)
                    {
                        if (bloque.Count == 2)
                        {
                            int a = bloque[0];
                            int b = bloque[1];
                            if (colocadosFila.Contains(a) || colocadosFila.Contains(b)) continue;

                            coordenadasPorCedula[a] = new GridCoord { Row = row, Col = colCursor };
                            coordenadasPorCedula[b] = new GridCoord { Row = row, Col = colCursor + 1 };

                            colocadosFila.Add(a);
                            colocadosFila.Add(b);
                            colCursor += 2;
                        }
                        else
                        {
                            int a = bloque[0];
                            if (colocadosFila.Contains(a)) continue;

                            coordenadasPorCedula[a] = new GridCoord { Row = row, Col = colCursor };
                            colocadosFila.Add(a);
                            colCursor += 1;
                        }
                    }

                    // Si este grupo colocó al menos un miembro y no es el último de la fila, insertamos una separación
                    bool grupoColocoAlgo = colocadosFila.Count > colocadosAntesDelGrupo;
                    if (grupoColocoAlgo)
                    {
                        gruposColocados++;
                        // Añadir una columna vacía entre grupos (no al final de todos si no hay más contenido)
                        colCursor += 1;
                    }
                }

                // 2) Miembros fuera de grupos (también como bloques)
                var miembrosFuera = cedulas.Except(gruposHermanos.SelectMany(g => g)).ToList();
                if (miembrosFuera.Count > 0)
                {
                    var bloquesFuera = CrearBloques(miembrosFuera);

                    foreach (var bloque in bloquesFuera)
                    {
                        if (bloque.Count == 2)
                        {
                            int a = bloque[0];
                            int b = bloque[1];
                            if (colocadosFila.Contains(a) || colocadosFila.Contains(b)) continue;

                            coordenadasPorCedula[a] = new GridCoord { Row = row, Col = colCursor };
                            coordenadasPorCedula[b] = new GridCoord { Row = row, Col = colCursor + 1 };

                            colocadosFila.Add(a);
                            colocadosFila.Add(b);
                            colCursor += 2;
                        }
                        else
                        {
                            int a = bloque[0];
                            if (colocadosFila.Contains(a)) continue;

                            coordenadasPorCedula[a] = new GridCoord { Row = row, Col = colCursor };
                            colocadosFila.Add(a);
                            colCursor += 1;
                        }
                    }
                }

                // === DEBUG: ver cómo quedó la fila tras fase 2 (bloques) ===
                Debug.WriteLine($"=== Fase 2 (fila {row}) final ===");
                var filaOrdenada = cedulas
                    .Where(ced => coordenadasPorCedula.ContainsKey(ced))
                    .OrderBy(ced => coordenadasPorCedula[ced].Col)
                    .ToList();
                foreach (var ced in filaOrdenada)
                {
                    var m = miembrosLista[indicePorCedula[ced]];
                    string pareja = (m.Pareja != null && cedulas.Contains(m.Pareja.Cedula)) ? m.Pareja.Nombre : "—";
                    Debug.WriteLine($"  {m.Nombre}({ced}) → Col {coordenadasPorCedula[ced].Col} | Pareja: {pareja}");
                }

            }

            // === DEBUG: imprimir coordenadas después de Fase 2 ===
            Debug.WriteLine("=== Coordenadas después de Fase 2 ===");
            foreach (var fila in cedulasPorFila.OrderBy(f => f.Key))
            {
                int row = fila.Key;
                Debug.WriteLine($"Fila {row}:");

                var cedulasOrdenadas = fila.Value
                    .OrderBy(ced => coordenadasPorCedula[ced].Col)
                    .ToList();

                foreach (var ced in cedulasOrdenadas)
                {
                    var miembro = miembrosLista[indicePorCedula[ced]];
                    string nombre = miembro.Nombre ?? $"Cedula {ced}";
                    string pareja = miembro.Pareja != null ? miembro.Pareja.Nombre : "—";

                    Debug.WriteLine(
                        $"  {nombre} (Ced: {ced}) → Col {coordenadasPorCedula[ced].Col} | Pareja: {pareja}"
                    );
                }
            }

            Debug.WriteLine("=====================================");



            // ==========================================================================================
            //         4. CENTRAR PADRES RESPECTO A SUS HIJOS (PROCESANDO FILAS EN REVERSA)
            // ==========================================================================================

            foreach (var grupoFila in cedulasPorFila.OrderByDescending(f => f.Key))
            {
                int filaActual = grupoFila.Key;
                var cedulasEnFila = grupoFila.Value;

                var cedulasEnFilaOrdenadas = cedulasEnFila.OrderBy(ced => coordenadasPorCedula[ced].Col).ToList();

                var colaSinHijos = new Queue<int>();
                var procesados = new HashSet<int>();
                double bordeDerechoFila = 0.0;
                bool huboPadresConHijos = false;

                for (int i = 0; i < cedulasEnFilaOrdenadas.Count; i++)
                {
                    int cedulaActual = cedulasEnFilaOrdenadas[i];
                    if (procesados.Contains(cedulaActual)) continue;

                    var miembroActual = miembrosLista[indicePorCedula[cedulaActual]];

                    // Caso 1: persona con hijos → calcular centro y colocar
                    if (miembroActual.Hijos.Count > 0)
                    {
                        huboPadresConHijos = true;

                        double minColHijos = double.PositiveInfinity;
                        double maxColHijos = double.NegativeInfinity;

                        foreach (var hijo in miembroActual.Hijos)
                        {
                            if (coordenadasPorCedula.ContainsKey(hijo.Cedula))
                            {
                                double colHijo = coordenadasPorCedula[hijo.Cedula].Col;
                                minColHijos = Math.Min(minColHijos, colHijo);
                                maxColHijos = Math.Max(maxColHijos, colHijo);

                                if (hijo.Pareja != null && coordenadasPorCedula.ContainsKey(hijo.Pareja.Cedula))
                                {
                                    double colParejaHijo = coordenadasPorCedula[hijo.Pareja.Cedula].Col;
                                    minColHijos = Math.Min(minColHijos, colParejaHijo);
                                    maxColHijos = Math.Max(maxColHijos, colParejaHijo);
                                }
                            }
                        }

                        int cantidadHijos = miembroActual.Hijos.Count;
                        int cantidadHijosConPareja = miembroActual.Hijos.Count(h => h.Pareja != null);
                        double columnaPadre = PromedioRangoConShift(minColHijos, maxColHijos);

                        // Colocar al padre centrado
                        coordenadasPorCedula[miembroActual.Cedula] = new GridCoord
                        {
                            Row = filaActual,
                            Col = columnaPadre
                        };
                        procesados.Add(miembroActual.Cedula);

                        bordeDerechoFila = Math.Max(bordeDerechoFila, columnaPadre);

                        // Si tiene pareja en la misma fila, colocarla contigua en este mismo paso
                        bool parejaEnMismaFila = miembroActual.Pareja != null &&
                                                 cedulasEnFilaOrdenadas.Contains(miembroActual.Pareja.Cedula);

                        if (parejaEnMismaFila)
                        {
                            int cedulaPareja = miembroActual.Pareja.Cedula;

                            coordenadasPorCedula[cedulaPareja] = new GridCoord
                            {
                                Row = filaActual,
                                Col = columnaPadre + 1.0
                            };

                            procesados.Add(cedulaPareja);

                            bordeDerechoFila = Math.Max(bordeDerechoFila, columnaPadre + 1.0);
                        }

                        // Actualizar borde derecho con el extremo real del bloque
                        bordeDerechoFila = Math.Max(bordeDerechoFila, maxColHijos + 1.0);
                    }
                    else
                    {
                        // Caso 2: sin hijos → no mutar coordenadas aquí, solo encolar
                        colaSinHijos.Enqueue(miembroActual.Cedula);
                    }
                }

                // Al terminar la fila:
                if (!huboPadresConHijos) continue; // no tocar nada en esta fila

                // Al terminar de recorrer la fila, colocamos las personas sin hijos a la derecha
                var yaColocados = new HashSet<int>();
                while (colaSinHijos.Count > 0)
                {
                    var cedulaSinHijos = colaSinHijos.Dequeue();
                    if (yaColocados.Contains(cedulaSinHijos)) continue;

                    var m = miembrosLista[indicePorCedula[cedulaSinHijos]];
                    bool parejaEnFila = m.Pareja != null && cedulasEnFilaOrdenadas.Contains(m.Pareja.Cedula);

                    // Siempre colocar justo después del borde derecho actual
                    double nuevaCol = bordeDerechoFila + 1.0;

                    if (parejaEnFila && !yaColocados.Contains(m.Pareja.Cedula))
                    {
                        int cedPareja = m.Pareja.Cedula;
                        coordenadasPorCedula[cedulaSinHijos] = new GridCoord { Row = filaActual, Col = nuevaCol };
                        coordenadasPorCedula[cedPareja] = new GridCoord { Row = filaActual, Col = nuevaCol + 1.0 };

                        yaColocados.Add(cedulaSinHijos);
                        yaColocados.Add(cedPareja);
                        bordeDerechoFila = nuevaCol + 1.0;
                    }
                    else
                    {
                        coordenadasPorCedula[cedulaSinHijos] = new GridCoord { Row = filaActual, Col = nuevaCol };
                        yaColocados.Add(cedulaSinHijos);
                        bordeDerechoFila = nuevaCol;
                    }
                }
            }

            // === DEBUG: imprimir coordenadas al terminar la fase 4 ===
            Debug.WriteLine("=== Coordenadas después de Fase 4 ===");
            foreach (var kvp in coordenadasPorCedula.OrderBy(k => k.Value.Row).ThenBy(k => k.Value.Col))
            {
                int cedula = kvp.Key;
                var coord = kvp.Value;
                var miembro = miembrosLista[indicePorCedula[cedula]];

                string nombre = miembro.Nombre ?? $"Cedula {cedula}";
                string pareja = miembro.Pareja != null ? miembro.Pareja.Nombre : "—";
                int hijos = miembro.Hijos.Count;

                Debug.WriteLine(
                    $"{nombre} (Ced: {cedula}) → Row {coord.Row}, Col {coord.Col} | Pareja: {pareja} | Hijos: {hijos}"
                );
            }
            Debug.WriteLine("=====================================");

            return coordenadasPorCedula;
        }

        /// <summary>
        /// Ordena un grupo de miembros asegurando que las parejas queden contiguas.
        /// Usa la matriz de relaciones (valor 4) para detectar parejas.
        /// </summary>
        private static List<int> OrdenarConParejas(
            List<int> grupo,
            List<MiembroFamilia> miembros,
            Dictionary<int, int> index,
            int[,] matriz)
        {
            // HashSet para registrar las cédulas que ya fueron emparejadas
            var parejas = new HashSet<int>();

            // Lista de salida con el orden final
            var orden = new List<int>();

            // Paso 1: recorrer cada miembro y buscar su pareja dentro del grupo
            foreach (var ced in grupo)
            {
                int i = index[ced];

                foreach (var otro in grupo)
                {
                    if (otro == ced) continue;

                    int j = index[otro];

                    if (matriz[i, j] == 4) // relación de pareja
                    {
                        // Si alguno ya fue registrado, saltar
                        if (parejas.Contains(ced) || parejas.Contains(otro))
                            continue;

                        parejas.Add(ced);
                        parejas.Add(otro);

                        // Agregar la pareja contigua al orden
                        orden.Add(ced);
                        orden.Add(otro);
                        break;
                    }
                }
            }

            // Paso 2: agregar los que no tienen pareja al final
            foreach (var ced in grupo)
            {
                if (!parejas.Contains(ced))
                    orden.Add(ced);
            }

            return orden;
        }

        /// <summary>
        /// Agrupa un conjunto de miembros según un tipo de relación en la matriz.
        /// Usa BFS para encontrar todos los conectados por la relación indicada.
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
                    var actual = cola.Dequeue();
                    int ia = index[actual];

                    // Explorar vecinos por la relación dada
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

        // Calcula la "posición media" del padre según la suma de columnas enteras
        // desde minCol (inclusive) hasta maxCol (inclusive), devuelve double.
        // Si minCol > maxCol devuelve (minCol+maxCol)/2 como fallback.
        public static double PromedioRangoConShift(double minCol, double maxCol)
        {
            double res = 0.0;
            double contador = 0.0;

            for (double i = minCol; i <= maxCol; i++)
            {
                res += i;
                contador += 1.0;
            }

            res = (res / contador) - 0.5;
            return res;
        }

    }
}