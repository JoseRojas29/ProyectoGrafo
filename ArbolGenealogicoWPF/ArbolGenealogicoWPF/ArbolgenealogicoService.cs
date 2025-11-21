using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

                LigarTodo(seleccionado);
                LigarTodo(nuevo);

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

            if (miembro.Padre != null && miembro.Madre != null)
            {
                miembro.LigarHijosConPadre(miembro.Madre, miembro.Padre);
                miembro.LigarHijosConMadre(miembro.Madre, miembro.Padre);
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
            var index = new Dictionary<int, int>();
            for (int i = 0; i < n; i++)
                index[miembros[i].Cedula] = i;

            // Detectar parejas antes de asignar pesos
            // DetectarParejas(miembros);

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