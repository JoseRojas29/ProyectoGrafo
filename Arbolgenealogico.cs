using System;
using System.Collections.Generic;
using System.Linq;
using ArbolGenealogico.Modelos;

public class ArbolGenealogico
{
    public MiembroFamilia? Raiz { get; private set; }

    // ================================
    // CONSTRUCTOR MEJORADO
    // ================================
    public ArbolGenealogico(MiembroFamilia? raiz = null)
    {
        Raiz = raiz;
    }

    // ================================
    // AGREGAR PRIMER NODO DEL ÁRBOL
    // ================================
    public bool AgregarPrimerNodo(MiembroFamilia miembro)
    {
        if (miembro == null)
            throw new ArgumentNullException(nameof(miembro));

        if (Raiz != null)
            return false; // Ya existe una raíz

        Raiz = miembro;
        return true;
    }

    // ==========================================
    // BÚSQUEDA POR CÉDULA
    // ==========================================
    public bool Contiene(string cedula) => Raiz != null && BuscarPorCedula(Raiz, cedula) != null;

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

    // ==========================================
    // DETECTAR CICLO EN LA DESCENDENCIA
    // ==========================================
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

    // ==========================================
    // CLASE AUXILIAR PARA EL GRAFO
    // ==========================================
    public class Arista
    {
        public MiembroFamilia A { get; set; }
        public MiembroFamilia B { get; set; }
        public int Peso { get; set; }
        public string Tipo { get; set; }
    }

    // ==========================================
    // DETECTAR PAREJAS AUTOMÁTICAMENTE
    // (Regla: mismos hijos = pareja)
    // ==========================================
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

    // ==========================================
    // GENERAR GRAFO COMPLETO CON PESOS
    // Pareja ↔ pareja = 1
    // Padre/Madre ↔ hijo = 2
    // Hermano ↔ hermano = 3
    // ==========================================
    public List<Arista> GenerarGrafo(List<MiembroFamilia> miembros)
    {
        var aristas = new List<Arista>();

        // ===================================
        // 1. Padre/Madre ↔ Hijo (peso 2)
        // ===================================
        foreach (var miembro in miembros)
        {
            foreach (var hijo in miembro.Hijos)
            {
                aristas.Add(new Arista
                {
                    A = miembro,
                    B = hijo,
                    Peso = 2,
                    Tipo = "Padre-Hijo"
                });
            }
        }

        // ===================================
        // 2. Detectar parejas automáticamente
        // ===================================
        DetectarParejas(miembros);

        foreach (var miembro in miembros)
        {
            if (miembro.Pareja != null)
            {
                if (!aristas.Any(a =>
                    (a.A == miembro && a.B == miembro.Pareja) ||
                    (a.B == miembro && a.A == miembro.Pareja)))
                {
                    aristas.Add(new Arista
                    {
                        A = miembro,
                        B = miembro.Pareja,
                        Peso = 1,
                        Tipo = "Pareja"
                    });
                }
            }
        }

        // ===================================
        // 3. Hermanos (peso 3)
        // ===================================
        foreach (var miembro in miembros)
        {
            var hijos = miembro.Hijos;

            for (int i = 0; i < hijos.Count; i++)
            {
                for (int j = i + 1; j < hijos.Count; j++)
                {
                    if (!aristas.Any(a =>
                        (a.A == hijos[i] && a.B == hijos[j]) ||
                        (a.A == hijos[j] && a.B == hijos[i])))
                    {
                        aristas.Add(new Arista
                        {
                            A = hijos[i],
                            B = hijos[j],
                            Peso = 3,
                            Tipo = "Hermanos"
                        });
                    }
                }
            }
        }

        return aristas;
    }
}
