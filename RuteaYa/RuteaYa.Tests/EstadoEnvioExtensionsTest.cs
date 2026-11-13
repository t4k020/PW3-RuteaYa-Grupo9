using RuteaYa.Entidades;
using Xunit;

namespace RuteaYa.Tests
{
    public class EstadoEnvioExtensionsTest
    {
        [Theory]
        [InlineData(EstadoEnvio.BuscandoConductor, "Buscando conductor")]
        [InlineData(EstadoEnvio.ConductorAsignado, "Conductor asignado")]
        [InlineData(EstadoEnvio.Retirado, "Retirado")]
        [InlineData(EstadoEnvio.EnCamino, "En camino")]
        [InlineData(EstadoEnvio.Entregado, "Entregado")]
        [InlineData(EstadoEnvio.Cancelado, "Cancelado")]
        public void ObtenerNombre_DeberiaTraducirCadaEstado(EstadoEnvio estado, string esperado)
        {
            // Act
            var nombre = estado.ObtenerNombre();

            // Assert
            Assert.Equal(esperado, nombre);
        }

        [Fact]
        public void ObtenerNombre_DeberiaDevolverEstadoDesconocido_CuandoElValorNoEsValido()
        {
            // Act
            var nombre = ((EstadoEnvio)99).ObtenerNombre();

            // Assert
            Assert.Equal("Estado desconocido", nombre);
        }
    }
}
