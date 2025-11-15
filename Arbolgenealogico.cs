using System;
using System.Collections.Generic;
using System.Linq;
using ArbolGenealogico.Modelos;

public class ArbolGenealogico
{
    public MiembroFamilia? Raiz { get; private set; }

    public ArbolGenealogico(MiembroFamilia? raiz = null)
    {
        Raiz = raiz;
    }

    // =======================================================
    //  AGREGAR PRIMER NODO DEL ÁRBOL (RAÍZ)
    // =======================================================
    public bool AgregarPrimerNodo(MiembroFamilia miembro)
    {
        if (miembro == null)
            throw new ArgumentNullException(nameof(miembro));

        if (Raiz != null)
            return false;

        Raiz = miembro;
        return true;
    }

    // =======================================================
    //  AGREGAR NODO SEGÚN RELACIÓN: hijo, padre, madre, pareja
    // =======================================================
    public bool AgregarNodo(string cedulaReferencia, MiembroFamilia nuevo, string tipoRelacion)
    {
        if (nuevo == null)
            throw new ArgumentNullException(nameof(nuevo));

        if (Raiz == null)
        {
            Raiz = nuevo; // si no existe raíz, se vuelve raíz
            return true;
        }

        MiembroFamilia? refMiembro = BuscarPorCedula(Raiz, cedulaReferencia);
        if (refMiembro == null)
            return false;

        tipoRelacion = tipoRelacion.ToLower();

        switch (tipoRelacion)
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

            default:
                throw new ArgumentException("Tipo de relación no válido. Use: hijo, padre, madre o pareja.");
        }
    }

    // =======================================================
    //  BÚSQUEDA POR CÉDULA
    // =======================================================
    public bool Contiene(string cedula) =>
        Raiz != null && BuscarPorCedula(Raiz, cedula) != null;

    public MiembroFamilia? BuscarPorCedula(MiembroFamilia nodo, string cedula)
    {
        if (nodo.Cedula == cedula)
            return nodo;

        foreach (var hijo in nodo.Hijos)
        {
            var encontrado = BuscarPorCedula(hijo, cedula);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }

    // =======================================================
    //  DETECTAR CICLOS EN DESCENDENCIA
    // =======================================================
    public bool DetectarCiclo(MiembroFamilia nodo, HashSet<string> visitados)
    {
        if (visitados.Contains(nodo.Cedula))
            return true;

        visitados.Add(nodo.Cedula);

        foreach (var hijo in nodo.Hijos)
        {
            if (DetectarCiclo(hijo, new HashSet<string>(visitados)))
                return true;
        }
        return false;
    }

    // =======================================================
    //  DETECTAR PAREJAS AUTOMÁTICAMENTE POR MISMOS HIJOS
    // =======================================================
    public void DetectarParejas(List<MiembroFamilia> miembros)
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
    public int[,] GenerarMatriz(List<MiembroFamilia> miembros)
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
