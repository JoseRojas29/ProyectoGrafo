using System;
using System.Collections.Generic;
using System.Linq;

namespace ArbolGenealogicoWPF
{
    /// <summary>
    /// Clase de servicio que maneja el árbol genealógico
    /// Solo interesan los métodos de esta clase para manipular el árbol
    /// </summary>
    public static class ArbolGenealogicoService
    {
        public static bool AgregarPrimerNodo(MiembroFamilia miembro)
        {
            NuevoNodo = miembro;
            return true;
        }

        // Agregar nodo según relación (ComboBox)
        public static bool AgregarNodo(MiembroFamilia seleccionado, MiembroFamilia nuevo, int relacion)
        {
            if (nuevo == null)
                throw new ArgumentNullException(nameof(nuevo));

            switch (relacion)
            {
                case "hijo":
                case "hija":
                case "descendiente":
                    refMiembro.Hijos.Add(nuevo);
                    nuevo.Padre = refMiembro.Padre;
                    nuevo.Madre = refMiembro.Madre;
                    return true;

                case "padre":
                    refMiembro.AsignarPadre(nuevo);
                    return true;

                case "madre":
                    refMiembro.AsignarMadre(nuevo);
                    return true;

                case "pareja":
                    refMiembro.AsignarPareja(nuevo);
                    return true;
            }
        }

        // =======================================================
        //  DETECTAR PAREJAS AUTOMÁTICAMENTE POR MISMOS HIJOS
        // =======================================================
        public static void DetectarParejas(List<MiembroFamilia> miembros)
        {
            for (int i = 0; i < miembros.Count; i++)
            {
                for (int j = i + 1; j < miembros.Count; j++)
                {
                    var a = miembros[i];
                    var b = miembros[j];

                    if (a.Hijos.Count == 0 || b.Hijos.Count == 0)
                        continue;

                    bool mismosHijos =
                        a.Hijos.All(h => b.Hijos.Contains(h)) &&
                        b.Hijos.All(h => a.Hijos.Contains(h));

                    if (mismosHijos)
                    {
                        a.AsignarPareja(b);
                    }
                }
            }
        }

        // =======================================================
        //  GENERAR MATRIZ DE ADYACENCIA CON PESOS
        //
        //  REGLAS:
        //  Padre/Madre → hijo = 2
        //  Hijo → padre/madre = 4
        //  Pareja ↔ pareja = 1
        //  Hermanos ↔ hermanos = 3
        // =======================================================
        public static int[,] GenerarMatriz(List<MiembroFamilia> miembros)
        {
            int n = miembros.Count;
            int[,] matriz = new int[n, n];

            // índice rápido por cédula
            var index = new Dictionary<string, int>();
            for (int i = 0; i < n; i++)
                index[miembros[i].Cedula] = i;

            // Detectar parejas antes de asignar pesos
            DetectarParejas(miembros);

            // =======================================================
            // 1. RELACIONES PADRE/MADRE ↔ HIJO
            // =======================================================
            foreach (var miembro in miembros)
            {
                int i = index[miembro.Cedula];

                foreach (var hijo in miembro.Hijos)
                {
                    int j = index[hijo.Cedula];

                    // ascendente → descendiente
                    matriz[i, j] = 2;

                    // descendiente → ascendente
                    matriz[j, i] = 4;
                }
            }

            // =======================================================
            // 2. PAREJAS = 1 en ambos sentidos
            // =======================================================
            foreach (var miembro in miembros)
            {
                if (miembro.Pareja == null)
                    continue;

                int a = index[miembro.Cedula];
                int b = index[miembro.Pareja.Cedula];

                matriz[a, b] = 1;
                matriz[b, a] = 1;
            }

            // =======================================================
            // 3. HERMANOS = 3 en ambos sentidos
            // =======================================================
            foreach (var miembro in miembros)
            {
                var hijos = miembro.Hijos;

                for (int x = 0; x < hijos.Count; x++)
                {
                    for (int y = x + 1; y < hijos.Count; y++)
                    {
                        int i = index[hijos[x].Cedula];
                        int j = index[hijos[y].Cedula];

                        matriz[i, j] = 3;
                        matriz[j, i] = 3;
                    }
                }
            }

            return matriz;
        }
    }
}