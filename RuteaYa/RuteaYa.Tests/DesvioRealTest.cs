using RuteaYa.Entidades;
using RuteaYa.Servicios;
using Xunit;

namespace RuteaYa.Tests
{
    public class DesvioRealTest
    {
        // Viaje que va de norte a sur por un mismo meridiano (misma longitud): sobre una
        // línea así, pasar por puntos intermedios no agrega kilómetros.
        private static Viaje ViajePorMeridiano(decimal distanciaPorCalleKm = 26m)
        {
            return new Viaje
            {
                OrigenLat = -34.50m,
                OrigenLng = -58.40m,
                DestinoLat = -34.70m,
                DestinoLng = -58.40m,
                DistanciaKm = distanciaPorCalleKm
            };
        }

        private static Envio EnvioEntre(
            decimal origenLat, decimal origenLng, decimal destinoLat, decimal destinoLng, int id = 1)
        {
            return new Envio
            {
                Id = id,
                OrigenLat = origenLat,
                OrigenLng = origenLng,
                DestinoLat = destinoLat,
                DestinoLng = destinoLng
            };
        }

        // =====================================================================
        // Distancia entre coordenadas
        // =====================================================================

        [Fact]
        public void DistanciaKm_MismoPunto_DeberiaSerCero()
        {
            var distancia = CalculoGeografico.DistanciaKm(-34.6037m, -58.3816m, -34.6037m, -58.3816m);

            Assert.Equal(0m, distancia);
        }

        [Fact]
        public void DistanciaKm_ObeliscoYPlazaDeMayo_DeberiaRondarUnKilometro()
        {
            var distancia = CalculoGeografico.DistanciaKm(-34.6037m, -58.3816m, -34.6083m, -58.3712m);

            Assert.InRange(distancia, 1.0m, 1.2m);
        }

        [Fact]
        public void DistanciaKm_DeberiaSerSimetrica()
        {
            var ida = CalculoGeografico.DistanciaKm(-34.68m, -58.51m, -34.61m, -58.48m);
            var vuelta = CalculoGeografico.DistanciaKm(-34.61m, -58.48m, -34.68m, -58.51m);

            Assert.Equal(ida, vuelta);
        }

        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(-34.6, -58.4, true)]
        [InlineData(0, -58.4, true)]
        [InlineData(-34.6, 0, true)]
        public void TieneCoordenadas_SoloElParCeroCeroEsInvalido(double lat, double lng, bool esperado)
        {
            Assert.Equal(esperado, CalculoGeografico.TieneCoordenadas((decimal)lat, (decimal)lng));
        }

        // =====================================================================
        // Desvío real de un envío respecto de un viaje
        // =====================================================================

        [Fact]
        public void CalcularDesvioKm_SinCoordenadasDelEnvio_DeberiaUsarLaEstimacion()
        {
            var viaje = ViajePorMeridiano();
            var envio = EnvioEntre(0, 0, 0, 0, id: 7);

            Assert.Equal(OptimizadorService.EstimarDesvioKm(7), OptimizadorService.CalcularDesvioKm(viaje, envio));
        }

        [Fact]
        public void CalcularDesvioKm_SinCoordenadasDelViaje_DeberiaUsarLaEstimacion()
        {
            var viaje = new Viaje { DistanciaKm = 26m };
            var envio = EnvioEntre(-34.55m, -58.40m, -34.65m, -58.40m, id: 3);

            Assert.Equal(OptimizadorService.EstimarDesvioKm(3), OptimizadorService.CalcularDesvioKm(viaje, envio));
        }

        [Fact]
        public void CalcularDesvioKm_EnvioSobreLaRutaDelViaje_DeberiaSerCero()
        {
            var viaje = ViajePorMeridiano();
            var envio = EnvioEntre(-34.55m, -58.40m, -34.65m, -58.40m);

            Assert.Equal(0m, OptimizadorService.CalcularDesvioKm(viaje, envio));
        }

        [Fact]
        public void CalcularDesvioKm_EnvioCasiSobreLaRuta_DeberiaSerUnDesvioChico()
        {
            var viaje = ViajePorMeridiano();
            var envio = EnvioEntre(-34.55m, -58.41m, -34.65m, -58.41m);

            var desvio = OptimizadorService.CalcularDesvioKm(viaje, envio);

            Assert.InRange(desvio, 0.10m, 0.30m);
        }

        [Fact]
        public void CalcularDesvioKm_EnvioLejosDeLaRuta_DeberiaSerUnDesvioGrande()
        {
            var viaje = ViajePorMeridiano();
            var envio = EnvioEntre(-34.55m, -58.55m, -34.65m, -58.55m);

            var desvio = OptimizadorService.CalcularDesvioKm(viaje, envio);

            Assert.InRange(desvio, 21m, 22m);
        }

        [Fact]
        public void CalcularDesvioKm_EnvioMasLejosDeberiaTenerMasDesvio()
        {
            var viaje = ViajePorMeridiano();
            var cerca = EnvioEntre(-34.55m, -58.41m, -34.65m, -58.41m);
            var lejos = EnvioEntre(-34.55m, -58.55m, -34.65m, -58.55m);

            Assert.True(
                OptimizadorService.CalcularDesvioKm(viaje, cerca)
                < OptimizadorService.CalcularDesvioKm(viaje, lejos));
        }

        [Fact]
        public void CalcularDesvioKm_EnvioEnSentidoContrario_DeberiaSumarElRecorridoDeIdaYVuelta()
        {
            var viaje = ViajePorMeridiano();
            var envio = EnvioEntre(-34.65m, -58.40m, -34.55m, -58.40m);

            var desvio = OptimizadorService.CalcularDesvioKm(viaje, envio);

            Assert.InRange(desvio, 25.5m, 26.5m);
        }

        [Fact]
        public void CalcularDesvioKm_ViajeMasSinuoso_DeberiaAumentarElDesvio()
        {
            var envio = EnvioEntre(-34.55m, -58.45m, -34.65m, -58.45m);

            var directo = OptimizadorService.CalcularDesvioKm(ViajePorMeridiano(28.9m), envio);
            var sinuoso = OptimizadorService.CalcularDesvioKm(ViajePorMeridiano(44.4m), envio);

            Assert.InRange(directo, 4.0m, 4.5m);
            Assert.InRange(sinuoso, 6.3m, 6.8m);
        }

        [Fact]
        public void CalcularDesvioKm_ViajeSinDistanciaGuardada_DeberiaUsarLaSinuosidadPorDefecto()
        {
            var envio = EnvioEntre(-34.55m, -58.45m, -34.65m, -58.45m);

            var sinDistancia = OptimizadorService.CalcularDesvioKm(ViajePorMeridiano(0m), envio);
            var conDistancia = OptimizadorService.CalcularDesvioKm(ViajePorMeridiano(28.9m), envio);

            // 28,9 km por calle equivale a la sinuosidad por defecto (1,3) en este viaje.
            Assert.InRange(sinDistancia, conDistancia - 0.02m, conDistancia + 0.02m);
        }

        [Fact]
        public void CalcularDesvioKm_ConDatosDelSeed_DeberiaDarElValorEsperado()
        {
            // Brandsen 805, CABA -> San Justo (26 km por calle) y el envío de La Tablada a Almagro.
            var viaje = new Viaje
            {
                OrigenLat = -34.6356m,
                OrigenLng = -58.3647m,
                DestinoLat = -34.681m,
                DestinoLng = -58.553m,
                DistanciaKm = 26m
            };
            var envio = EnvioEntre(-34.68m, -58.51m, -34.61m, -58.48m);

            var desvio = OptimizadorService.CalcularDesvioKm(viaje, envio);

            Assert.InRange(desvio, 21.3m, 21.6m);
        }

        [Fact]
        public void CalcularDesvioKm_DeberiaRedondearADosDecimales()
        {
            var viaje = ViajePorMeridiano();
            var envio = EnvioEntre(-34.55m, -58.4123m, -34.65m, -58.4377m);

            var desvio = OptimizadorService.CalcularDesvioKm(viaje, envio);

            Assert.Equal(Math.Round(desvio, 2), desvio);
        }
    }
}
