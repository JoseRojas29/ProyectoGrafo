using System;

public class ArbolGenealogico
{
    public MiembroFamilia Raiz { get; private set; }

    public ArbolGenealogico(MiembroFamilia raiz)
    {
        Raiz = raiz;
    }

    public bool Contiene(string cedula) => BuscarPorCedula(Raiz, cedula) != null;

    public MiembroFamilia? BuscarPorCedula(MiembroFamilia nodo, string cedula)
    {
        if (nodo == null) return null;
        if (nodo.Cedula == cedula) return nodo;

        foreach (var hijo in nodo.Hijos)
        {
            var encontrado = BuscarPorCedula(hijo, cedula);
            if (encontrado != null) return encontrado;
        }
        return null;
    }

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
}

