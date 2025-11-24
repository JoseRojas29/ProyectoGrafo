using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace ArbolGenealogicoWPF
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

        public IReadOnlyList<MiembroFamilia> Miembros => _miembros;
        public IReadOnlyList<AristaGeografica> Aristas => _aristas;

        public GrafoGeografico(IEnumerable<MiembroFamilia> miembros)
        {
            if (miembros == null) throw new ArgumentNullException(nameof(miembros));

            _miembros = miembros
                .Where(m => m != null)
                .Distinct()
                .ToList();

            _aristas = new List<AristaGeografica>();

            ConstruirGrafoCompleto();
        }

        private void ConstruirGrafoCompleto()
        {
            _aristas.Clear();

            for (int i = 0; i < _miembros.Count; i++)
            {
                var origen = _miembros[i];

                // Intentamos obtener lat/lon del miembro origen; si no hay, lo omitimos.
                if (!TryGetLatLon(origen, out double lat1, out double lon1))
                    continue;

                for (int j = i + 1; j < _miembros.Count; j++)
                {
                    var destino = _miembros[j];

                    if (!TryGetLatLon(destino, out double lat2, out double lon2))
                        continue;

                    double distanciaKm = DistanciaGeografica.CalcularDistanciaKm(lat1, lon1, lat2, lon2);

                    var arista = new AristaGeografica(origen, destino, distanciaKm);
                    _aristas.Add(arista);
                }
            }
        }

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

        // Firma que pediste: la cedula ya es int y no la validamos aquí.
        public List<(MiembroFamilia destino, double distanciaKm)> ObtenerDistanciasDesdeCedula(int cedula)
        {
            var origen = _miembros.FirstOrDefault(m => m.Cedula == cedula);

            if (origen == null)
                return new List<(MiembroFamilia, double)>();

            return ObtenerDistanciasDesde(origen);
        }

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

        // -------------------
        // Helper: Extrae lat/lon de un MiembroFamilia de forma tolerante.
        // Soporta:
        //  - propiedades públicas 'Latitud' y 'Longitud' de tipo double (preferido)
        //  - propiedad 'CoordenadasResidencia' como string "lat,lon"
        //  - o ValueTuple<double,double> si existiera (no es tu caso actual, pero lo cubre)
        // -------------------
        private static bool TryGetLatLon(MiembroFamilia m, out double lat, out double lon)
        {
            lat = 0;
            lon = 0;
            if (m == null) return false;

            var t = m.GetType();

            // 1) Propiedades Latitud / Longitud
            var propLat = t.GetProperty("Latitud", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                          ?? t.GetProperty("Latitude", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var propLon = t.GetProperty("Longitud", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                          ?? t.GetProperty("Longitude", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propLat != null && propLon != null)
            {
                var vLat = propLat.GetValue(m);
                var vLon = propLon.GetValue(m);

                if (vLat is double dvLat && vLon is double dvLon)
                {
                    lat = dvLat; lon = dvLon;
                    return true;
                }

                // si vienen como string numérico
                if (vLat is string sLat && vLon is string sLon &&
                    double.TryParse(sLat, NumberStyles.Float, CultureInfo.InvariantCulture, out double pLat) &&
                    double.TryParse(sLon, NumberStyles.Float, CultureInfo.InvariantCulture, out double pLon))
                {
                    lat = pLat; lon = pLon;
                    return true;
                }
            }

            // 2) Propiedad CoordenadasResidencia (podría ser string "lat,lon" o tupla)
            var propCoords = t.GetProperty("CoordenadasResidencia", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propCoords != null)
            {
                var v = propCoords.GetValue(m);
                if (v == null) return false;

                // si es ValueTuple<double,double>
                if (v is ValueTuple<double, double> vt)
                {
                    lat = vt.Item1; lon = vt.Item2;
                    return true;
                }

                // si es algún ValueTuple boxed (ej. System.ValueTuple`2), intentar leer Item1/Item2
                var vtType = v.GetType();
                if (vtType.IsValueType && vtType.FullName?.StartsWith("System.ValueTuple") == true)
                {
                    try
                    {
                        var f1 = vtType.GetField("Item1") ?? (MemberInfo)vtType.GetProperty("Item1");
                        var f2 = vtType.GetField("Item2") ?? (MemberInfo)vtType.GetProperty("Item2");

                        object raw1 = null, raw2 = null;
                        if (f1 is FieldInfo fi1) raw1 = fi1.GetValue(v);
                        else if (f1 is PropertyInfo pi1) raw1 = pi1.GetValue(v);
                        if (f2 is FieldInfo fi2) raw2 = fi2.GetValue(v);
                        else if (f2 is PropertyInfo pi2) raw2 = pi2.GetValue(v);

                        if (raw1 is double rd1 && raw2 is double rd2)
                        {
                            lat = rd1; lon = rd2;
                            return true;
                        }
                    }
                    catch { /* ignore */ }
                }

                // si es string "lat,lon"
                if (v is string s)
                {
                    var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double p1) &&
                        double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double p2))
                    {
                        lat = p1; lon = p2;
                        return true;
                    }
                }
            }

            // 3) Propiedades genéricas 'Coordenadas' o 'Coords' como string
            var propGeneric = t.GetProperty("Coordenadas", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                              ?? t.GetProperty("Coords", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propGeneric != null)
            {
                var gv = propGeneric.GetValue(m);
                if (gv is string gs)
                {
                    var parts = gs.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double p1) &&
                        double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double p2))
                    {
                        lat = p1; lon = p2;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
