using System;

namespace ArbolGenealogicoWPF
{
    /// <summary>
    /// Utilidades para cálculo de distancias geográficas usando la fórmula de Haversine.
    /// Todas las distancias se devuelven en kilómetros.
    /// </summary>
    public static class DistanciaGeografica
    {
        // Radio medio de la Tierra en kilómetros
        private const double RadioTierraKm = 6371.0;

        /// <summary>
        /// Calcula la distancia en kilómetros entre dos puntos geográficos.
        /// Las coordenadas se expresan en grados decimales.
        /// </summary>
        /// <param name="latitud1">Latitud del primer punto en grados decimales.</param>
        /// <param name="longitud1">Longitud del primer punto en grados decimales.</param>
        /// <param name="latitud2">Latitud del segundo punto en grados decimales.</param>
        /// <param name="longitud2">Longitud del segundo punto en grados decimales.</param>
        /// <returns>Distancia aproximada en kilómetros.</returns>
        public static double CalcularDistanciaKm(
            double latitud1,
            double longitud1,
            double latitud2,
            double longitud2)
        {
            // Si son exactamente las mismas coordenadas, ahorramos cálculo
            if (latitud1 == latitud2 && longitud1 == longitud2)
                return 0.0;

            double lat1Rad = GradosARadianes(latitud1);
            double lon1Rad = GradosARadianes(longitud1);
            double lat2Rad = GradosARadianes(latitud2);
            double lon2Rad = GradosARadianes(longitud2);

            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            double sinLat = Math.Sin(dLat / 2.0);
            double sinLon = Math.Sin(dLon / 2.0);

            double a =
                sinLat * sinLat +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * sinLon * sinLon;

            // Por posibles errores de redondeo, aseguramos que 'a' quede en [0,1]
            a = Math.Clamp(a, 0.0, 1.0);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            double distancia = RadioTierraKm * c;

            return distancia;
        }

        /// <summary>
        /// Convierte grados a radianes.
        /// </summary>
        private static double GradosARadianes(double grados)
        {
            return (Math.PI / 180.0) * grados;
        }
    }
}
