namespace RuteaYa.Servicios
{
    // Cálculos de distancia entre coordenadas reales (latitud y longitud en grados).
    public static class CalculoGeografico
    {
        private const double RadioTierraKm = 6371d;

        // Los datos viejos guardan las coordenadas en 0; ese par no es un lugar real.
        public static bool TieneCoordenadas(decimal lat, decimal lng)
        {
            return lat != 0m || lng != 0m;
        }

        // Distancia en línea recta entre dos puntos (fórmula de Haversine).
        public static decimal DistanciaKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            var deltaLat = GradosARadianes((double)(lat2 - lat1));
            var deltaLng = GradosARadianes((double)(lng2 - lng1));

            var latitud1 = GradosARadianes((double)lat1);
            var latitud2 = GradosARadianes((double)lat2);

            var senoLat = Math.Sin(deltaLat / 2);
            var senoLng = Math.Sin(deltaLng / 2);

            var a = senoLat * senoLat
                    + Math.Cos(latitud1) * Math.Cos(latitud2) * senoLng * senoLng;

            var angulo = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (decimal)(RadioTierraKm * angulo);
        }

        private static double GradosARadianes(double grados)
        {
            return grados * Math.PI / 180d;
        }
    }
}
