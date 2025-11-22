using System;
using System.Collections.Generic;
using System.Linq;
using ArbolGenealogico.Modelos;

namespace ArbolGenealogico.Geografia
{
    /// <summary>
    /// Representa una arista en el grafo geográfico entre dos miembros de la familia.
    /// </summary>
    public class AristaGeografica
    {
        public MiembroFamilia A { get; }
        public MiembroFamilia B { get; }
        public double DistanciaKm { get; }

        public AristaGeografica(MiembroFamilia a, MiembroFamilia b, double distanciaKm)
        {
            A = a ?? throw new ArgumentNullException(nameof(a));
            B = b ?? throw new ArgumentNullException(nameof(b));

            if (ReferenceEquals(a, b))
                throw new ArgumentException("No se puede crear una arista con el mismo miembro en ambos extremos.");

            if (distanciaKm < 0)
                throw new ArgumentOutOfRangeException(nameof(distanciaKm), "La distancia no puede ser negativa.");

            DistanciaKm = distanciaKm;
        }
    }

    /// <summary>
    /// Grafo completo de distancias geográficas entre miembros de la familia.
    /// Construye todas las distancias entre cada par de MiembroFamilia.
    /// </summary>
    public class GrafoGeografico
    {
        private readonly List<MiembroFamilia> _miembros;
        private readonly List<AristaGeografica> _aristas;

        /// <summary>
        /// Miembros incluidos en este grafo.
        /// </summary>
        public IReadOnlyList<MiembroFamilia> Miembros => _miembros;

        /// <summary>
        /// Aristas (pares de miembros + distancia) del grafo.
        /// </summary>
        public IReadOnlyList<AristaGeografica> Aristas => _aristas;

        /// <summary>
        /// Crea un grafo geográfico completo a partir de una colección de miembros.
        /// </summary>
        /// <param name="miembros">Colección de MiembroFamilia con coordenadas de residencia.</param>
        public GrafoGeografico(IEnumerable<MiembroFamilia> miembros)
        {
            if (miembros == null) throw new ArgumentNullException(nameof(miembros));

            // Filtramos nulos y quitamos duplicados por referencia.
            _miembros = miembros
                .Where(m => m != null)
                .Distinct()
                .ToList();

            _aristas = new List<AristaGeografica>();

            ConstruirGrafoCompleto();
        }

        /// <summary>
        /// Reconstruye el grafo asumiendo que la lista de miembros ya está cargada.
        /// Crea un grafo completo (todas las combinaciones i &lt; j).
        /// </summary>
        private void ConstruirGrafoCompleto()
        {
            _aristas.Clear();

            for (int i = 0; i < _miembros.Count; i++)
            {
                var origen = _miembros[i];
                var (lat1, lon1) = origen.CoordenadasResidencia;

                for (int j = i + 1; j < _miembros.Count; j++)
                {
                    var destino = _miembros[j];
                    var (lat2, lon2) = destino.CoordenadasResidencia;

                    double distanciaKm = DistanciaGeografica.CalcularDistanciaKm(lat1, lon1, lat2, lon2);

                    var arista = new AristaGeografica(origen, destino, distanciaKm);
                    _aristas.Add(arista);
                }
            }
        }

        /// <summary>
        /// Reemplaza la lista de miembros y reconstruye el grafo completo.
        /// Útil si la lista de MiembroFamilia cambia significativamente.
        /// </summary>
        public void ActualizarMiembros(IEnumerable<MiembroFamilia> nuevosMiembros)
        {
            if (nuevosMiembros == null) throw new ArgumentNullException(nameof(nuevosMiembros));

            _miembros.Clear();
            _miembros.AddRange(
                nuevosMiembros
                    .Where(m => m != null)
                    .Distinct()
            );

            ConstruirGrafoCompleto();
        }

        /// <summary>
        /// Obtiene las distancias desde un miembro origen a todos los demás miembros del grafo.
        /// </summary>
        /// <param name="origen">Miembro origen desde el cual se calculan las distancias.</param>
        /// <returns>
        /// Lista de tuplas (destino, distanciaKm), ordenada por distancia ascendente.
        /// </returns>
        public List<(MiembroFamilia destino, double distanciaKm)> ObtenerDistanciasDesde(MiembroFamilia origen)
        {
            if (origen == null) throw new ArgumentNullException(nameof(origen));

            if (!_miembros.Contains(origen))
                throw new ArgumentException("El miembro origen no forma parte de este grafo.", nameof(origen));

            var resultado = _aristas
                .Where(a => ReferenceEquals(a.A, origen) || ReferenceEquals(a.B, origen))
                .Select(a => (
                    destino: ReferenceEquals(a.A, origen) ? a.B : a.A,
                    distanciaKm: a.DistanciaKm
                ))
                .OrderBy(x => x.distanciaKm)
                .ToList();

            return resultado;
        }

        /// <summary>
        /// Versión de conveniencia: obtiene distancias desde la cédula de un miembro.
        /// Si la cédula no existe en el grafo, devuelve una lista vacía.
        /// </summary>
        public List<(MiembroFamilia destino, double distanciaKm)> ObtenerDistanciasDesdeCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                throw new ArgumentException("La cédula no puede ser nula o vacía.", nameof(cedula));

            var origen = _miembros
                .FirstOrDefault(m => string.Equals(m.Cedula, cedula, StringComparison.OrdinalIgnoreCase));

            if (origen == null)
                return new List<(MiembroFamilia, double)>();

            return ObtenerDistanciasDesde(origen);
        }

        /// <summary>
        /// Obtiene el par de miembros que viven más cerca entre sí.
        /// Devuelve null si no hay suficientes miembros para formar al menos una arista.
        /// </summary>
        public (MiembroFamilia A, MiembroFamilia B, double DistanciaKm)? ObtenerParMasCercano()
        {
            var arista = _aristas
                .Where(a => !double.IsNaN(a.DistanciaKm) && !double.IsInfinity(a.DistanciaKm))
                .OrderBy(a => a.DistanciaKm)
                .FirstOrDefault();

            if (arista == null)
                return null;

            return (arista.A, arista.B, arista.DistanciaKm);
        }

        /// <summary>
        /// Obtiene el par de miembros que viven más lejos entre sí.
        /// Devuelve null si no hay suficientes miembros para formar al menos una arista.
        /// </summary>
        public (MiembroFamilia A, MiembroFamilia B, double DistanciaKm)? ObtenerParMasLejano()
        {
            var arista = _aristas
                .Where(a => !double.IsNaN(a.DistanciaKm) && !double.IsInfinity(a.DistanciaKm))
                .OrderByDescending(a => a.DistanciaKm)
                .FirstOrDefault();

            if (arista == null)
                return null;

            return (arista.A, arista.B, arista.DistanciaKm);
        }

        /// <summary>
        /// Calcula la distancia promedio entre todos los pares de miembros del grafo.
        /// Si no hay aristas, devuelve 0.
        /// </summary>
        public double ObtenerDistanciaPromedio()
        {
            var distanciasValidas = _aristas
                .Select(a => a.DistanciaKm)
                .Where(d => !double.IsNaN(d) && !double.IsInfinity(d))
                .ToList();

            if (distanciasValidas.Count == 0)
                return 0.0;

            return distanciasValidas.Average();
        }
    }
}
