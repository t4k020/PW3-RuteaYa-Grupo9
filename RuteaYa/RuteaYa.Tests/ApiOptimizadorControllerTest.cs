using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Xunit;

namespace RuteaYa.Tests
{
    public class ApiOptimizadorControllerTest
    {
        private static readonly DateTime Fecha = new DateTime(2030, 1, 15);

        private readonly Mock<IOptimizadorService> _optimizadorServiceMock;
        private readonly ApiOptimizadorController _controller;

        public ApiOptimizadorControllerTest()
        {
            _optimizadorServiceMock = new Mock<IOptimizadorService>();
            _controller = new ApiOptimizadorController(_optimizadorServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        // ---------- GET: Optimizar ----------

        [Fact]
        public async Task Optimizar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            var resultado = await _controller.Optimizar(1, Fecha, CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(resultado);
            _optimizadorServiceMock.Verify(
                s => s.OptimizarAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Optimizar_DeberiaRetornarOkConLaOptimizacion()
        {
            ConfigurarUsuarioAutenticado();
            var optimizacion = new OptimizacionDto { ViajeId = 5, FechaViaje = Fecha, CandidatosEvaluados = 3 };
            _optimizadorServiceMock
                .Setup(s => s.OptimizarAsync(5, Fecha, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(optimizacion);

            var resultado = await _controller.Optimizar(5, Fecha, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Same(optimizacion, ok.Value);
        }

        [Fact]
        public async Task Optimizar_DeberiaUsarElUsuarioDelTokenYLosParametrosRecibidos()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.OptimizarAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OptimizacionDto());

            await _controller.Optimizar(7, Fecha, CancellationToken.None);

            _optimizadorServiceMock.Verify(
                s => s.OptimizarAsync(7, Fecha, 1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Optimizar_DeberiaRetornarNotFound_CuandoElViajeNoExisteOEsDeOtroUsuario()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.OptimizarAsync(999, Fecha, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((OptimizacionDto?)null);

            var resultado = await _controller.Optimizar(999, Fecha, CancellationToken.None);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Optimizar_DeberiaRetornarBadRequest_CuandoElServicioRechazaLaSolicitud()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.OptimizarAsync(1, Fecha, 1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("El viaje no se encuentra activo."));

            var resultado = await _controller.Optimizar(1, Fecha, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal(400, badRequest.StatusCode);
            var detalle = Assert.IsType<ProblemDetails>(badRequest.Value);
            Assert.Equal("No se puede optimizar el viaje.", detalle.Title);
            Assert.Equal("El viaje no se encuentra activo.", detalle.Detail);
        }

        [Fact]
        public async Task Optimizar_DeberiaRetornar500_CuandoOcurreUnErrorInesperado()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.OptimizarAsync(1, Fecha, 1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Fallo de base de datos"));

            var resultado = await _controller.Optimizar(1, Fecha, CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, objectResult.StatusCode);
        }

        // ---------- POST: Reservar ----------

        [Fact]
        public async Task Reservar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            var resultado = await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(resultado);
            _optimizadorServiceMock.Verify(
                s => s.ReservarCombinacionAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Reservar_DeberiaRetornarValidationProblem_CuandoModelStateEsInvalido()
        {
            ConfigurarUsuarioAutenticado();
            _controller.ModelState.AddModelError("EnviosIds", "Elegí al menos un envío.");

            var resultado = await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(resultado);
            Assert.IsType<ValidationProblemDetails>(objectResult.Value);
            _optimizadorServiceMock.Verify(
                s => s.ReservarCombinacionAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Reservar_DeberiaRetornarNoContent_CuandoLaReservaEsCorrecta()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.ReservarCombinacionAsync(
                    5, Fecha, It.IsAny<IReadOnlyCollection<int>>(), 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var resultado = await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task Reservar_DeberiaEnviarAlServicioLosEnviosElegidosYElUsuarioDelToken()
        {
            ConfigurarUsuarioAutenticado();
            IReadOnlyCollection<int>? enviados = null;
            _optimizadorServiceMock
                .Setup(s => s.ReservarCombinacionAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<int, DateTime, IReadOnlyCollection<int>, int, CancellationToken>(
                    (_, _, ids, _, _) => enviados = ids)
                .ReturnsAsync(true);

            await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            Assert.Equal(new[] { 3, 1, 8 }, enviados);
            _optimizadorServiceMock.Verify(
                s => s.ReservarCombinacionAsync(
                    5, Fecha, It.IsAny<IReadOnlyCollection<int>>(), 1, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Reservar_DeberiaRetornarNotFound_CuandoElViajeNoExisteOEsDeOtroUsuario()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.ReservarCombinacionAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var resultado = await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Reservar_DeberiaRetornarBadRequest_CuandoElServicioRechazaLaReserva()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.ReservarCombinacionAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(
                    "Otro conductor tomó un envío o ya no está disponible. Recalculá la combinación."));

            var resultado = await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal(400, badRequest.StatusCode);
            var detalle = Assert.IsType<ProblemDetails>(badRequest.Value);
            Assert.Equal("No se puede reservar la combinación.", detalle.Title);
            Assert.Equal(
                "Otro conductor tomó un envío o ya no está disponible. Recalculá la combinación.",
                detalle.Detail);
        }

        [Fact]
        public async Task Reservar_DeberiaRetornar500_CuandoOcurreUnErrorInesperado()
        {
            ConfigurarUsuarioAutenticado();
            _optimizadorServiceMock
                .Setup(s => s.ReservarCombinacionAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<int>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Fallo de base de datos"));

            var resultado = await _controller.Reservar(CrearSolicitudValida(), CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, objectResult.StatusCode);
        }

        // ---------- Validaciones de ReservarCombinacionDto ----------

        [Fact]
        public void ReservarCombinacionDto_DeberiaSerValido_CuandoTieneViajeFechaYEnvios()
        {
            var errores = Validar(CrearSolicitudValida());

            Assert.Empty(errores);
        }

        [Fact]
        public void ReservarCombinacionDto_DeberiaSerInvalido_CuandoNoTieneEnvios()
        {
            var solicitud = CrearSolicitudValida();
            solicitud.EnviosIds = new List<int>();

            var errores = Validar(solicitud);

            Assert.Contains(errores, e => e.ErrorMessage == "Elegí al menos un envío.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ReservarCombinacionDto_DeberiaSerInvalido_CuandoElViajeNoEsValido(int viajeId)
        {
            var solicitud = CrearSolicitudValida();
            solicitud.ViajeId = viajeId;

            var errores = Validar(solicitud);

            Assert.Contains(errores, e => e.ErrorMessage == "El viaje es obligatorio.");
        }

        // ---------- Métodos auxiliares ----------

        private static ReservarCombinacionDto CrearSolicitudValida()
        {
            return new ReservarCombinacionDto
            {
                ViajeId = 5,
                FechaViaje = Fecha,
                EnviosIds = new List<int> { 3, 1, 8 }
            };
        }

        private static List<ValidationResult> Validar(ReservarCombinacionDto dto)
        {
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);
            return resultados;
        }

        private void ConfigurarUsuarioAutenticado()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "usuario_test")
            }, "mock"));

            _controller.ControllerContext.HttpContext.User = user;
        }
    }
}
