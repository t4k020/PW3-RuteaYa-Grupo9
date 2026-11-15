using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Dtos;
using System.Security.Claims;
using Xunit;

namespace RuteaYa.Tests
{
    public class ApiEnvioControllerTest
    {
        private readonly Mock<IEnvioService> _envioServiceMock;
        private readonly ApiEnvioController _controller;

        public ApiEnvioControllerTest()
        {
            _envioServiceMock = new Mock<IEnvioService>();
            _controller = new ApiEnvioController(_envioServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task CrearEnvio_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            var resultado = await _controller.CrearEnvio(CrearSolicitudValida());

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task CrearEnvio_DeberiaRetornarBadRequest_CuandoModelStateEsInvalido()
        {
            ConfigurarUsuarioAutenticado();
            _controller.ModelState.AddModelError("OrigenDir", "La dirección de origen es obligatoria.");

            var resultado = await _controller.CrearEnvio(CrearSolicitudValida());

            var badRequestResult = Assert.IsType<ObjectResult>(resultado);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CrearEnvio_DeberiaRetornarBadRequest_CuandoElServicioRechazaLaOperacion()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.CrearEnvioAsync(It.IsAny<Envio>(), 1))
                .ThrowsAsync(new InvalidOperationException("El usuario asociado no existe."));

            var resultado = await _controller.CrearEnvio(CrearSolicitudValida());

            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task CrearEnvio_DeberiaRetornarCreated_CuandoLaOperacionEsCorrecta()
        {
            ConfigurarUsuarioAutenticado();
            var envioCreado = new Envio
            {
                Id = 1,
                OrigenDir = "Mitre 1000",
                DestinoDir = "Belgrano 500"
            };
            _envioServiceMock
                .Setup(s => s.CrearEnvioAsync(It.IsAny<Envio>(), 1))
                .ReturnsAsync(envioCreado);

            var resultado = await _controller.CrearEnvio(CrearSolicitudValida());

            var createdResult = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(201, createdResult.StatusCode);
            var envioRetornado = Assert.IsType<EnvioDto>(createdResult.Value);
            Assert.Equal(envioCreado.Id, envioRetornado.Id);
            Assert.Equal(envioCreado.OrigenDir, envioRetornado.OrigenDir);
            Assert.Equal(envioCreado.DestinoDir, envioRetornado.DestinoDir);
        }

        [Fact]
        public async Task ObtenerPorIdApi_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            var resultado = await _controller.ObtenerPorIdApi(1);

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPorIdApi_DeberiaRetornarOkConDto_CuandoElEnvioExiste()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ObtenerDetalleDtoAsync(
                    1,
                    1,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EnvioDto
                {
                    Id = 1,
                    OrigenDir = "Mitre 1000",
                    DestinoDir = "Belgrano 500"
                });

            var resultado = await _controller.ObtenerPorIdApi(1);

            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var envioRetornado = Assert.IsType<EnvioDto>(okResult.Value);
            Assert.Equal(1, envioRetornado.Id);
            Assert.Equal("Mitre 1000", envioRetornado.OrigenDir);
        }

        [Fact]
        public async Task ObtenerPorIdApi_DeberiaRetornarNotFound_CuandoElEnvioNoExiste()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ObtenerDetalleDtoAsync(
                    999,
                    1,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((EnvioDto?)null);

            var resultado = await _controller.ObtenerPorIdApi(999);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPorIdApi_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ObtenerDetalleDtoAsync(
                    It.IsAny<int>(),
                    1,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var resultado = await _controller.ObtenerPorIdApi(1);

            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task EliminarEnvioApi_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            var resultado = await _controller.EliminarEnvioApi(1);

            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task EliminarEnvioApi_DeberiaRetornarNoContent_CuandoElEnvioExisteYSeElimina()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.EliminarEnvioAsync(1, 1))
                .ReturnsAsync(true);

            var resultado = await _controller.EliminarEnvioApi(1);

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task EliminarEnvioApi_DeberiaRetornarNotFound_CuandoElEnvioNoExiste()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.EliminarEnvioAsync(999, 1))
                .ReturnsAsync(false);

            var resultado = await _controller.EliminarEnvioApi(999);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task EliminarEnvioApi_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.EliminarEnvioAsync(It.IsAny<int>(), 1))
                .ThrowsAsync(new Exception("Error de base de datos"));

            var resultado = await _controller.EliminarEnvioApi(1);

            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task CancelarEnvioApi_DeberiaRetornarNoContent_CuandoElServicioCancela()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.CancelarEnvioAsync(1, 1))
                .ReturnsAsync(true);

            var resultado = await _controller.CancelarEnvioApi(1);

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task CancelarEnvioApi_DeberiaRetornarBadRequest_CuandoElEstadoNoPermiteCancelar()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.CancelarEnvioAsync(1, 1))
                .ThrowsAsync(new InvalidOperationException("No se puede cancelar."));

            var resultado = await _controller.CancelarEnvioApi(1);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal(400, badRequest.StatusCode);
        }

        private static CrearEnvioDto CrearSolicitudValida()
        {
            return new CrearEnvioDto
            {
                OrigenDir = "Mitre 1000",
                DestinoDir = "Belgrano 500",
                FechaLimite = DateTime.Today.AddDays(1),
                PesoKg = 2,
                Tamano = 1,
                Descripcion = "Documentacion",
                Monto = 4000
            };
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

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            var resultado = await _controller.ActualizarEnvioApi(1, CrearSolicitudValida());

            Assert.IsType<UnauthorizedResult>(resultado);
            _envioServiceMock.Verify(
                s => s.ActualizarEnvioAsync(It.IsAny<int>(), It.IsAny<Envio>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaRetornarValidationProblem_CuandoModelStateEsInvalido()
        {
            ConfigurarUsuarioAutenticado();
            _controller.ModelState.AddModelError("OrigenDir", "El origen es obligatorio.");

            var resultado = await _controller.ActualizarEnvioApi(1, CrearSolicitudValida());

            var objectResult = Assert.IsType<ObjectResult>(resultado);
            Assert.IsType<ValidationProblemDetails>(objectResult.Value);
            _envioServiceMock.Verify(
                s => s.ActualizarEnvioAsync(It.IsAny<int>(), It.IsAny<Envio>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaRetornarNoContent_CuandoLaActualizacionEsCorrecta()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ActualizarEnvioAsync(5, It.IsAny<Envio>(), 1))
                .ReturnsAsync(true);

            var resultado = await _controller.ActualizarEnvioApi(5, CrearSolicitudValida());

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaEnviarAlServicioSoloLosDatosEditables()
        {
            ConfigurarUsuarioAutenticado();
            Envio? enviado = null;
            _envioServiceMock
                .Setup(s => s.ActualizarEnvioAsync(5, It.IsAny<Envio>(), 1))
                .Callback<int, Envio, int>((_, envio, _) => enviado = envio)
                .ReturnsAsync(true);
            var solicitud = CrearSolicitudValida();

            await _controller.ActualizarEnvioApi(5, solicitud);

            Assert.NotNull(enviado);
            Assert.Equal(solicitud.OrigenDir, enviado!.OrigenDir);
            Assert.Equal(solicitud.DestinoDir, enviado.DestinoDir);
            Assert.Equal(solicitud.FechaLimite, enviado.FechaLimite);
            Assert.Equal(solicitud.PesoKg, enviado.PesoKg);
            Assert.Equal(solicitud.Tamano, enviado.Tamano);
            Assert.Equal(solicitud.Descripcion, enviado.Descripcion);
            Assert.Equal(solicitud.Monto, enviado.Monto);
            // UsuarioId y Estado nunca los define el cliente.
            Assert.Equal(0, enviado.UsuarioId);
            Assert.Equal(0, enviado.Estado);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaUsarElUsuarioDelToken()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ActualizarEnvioAsync(It.IsAny<int>(), It.IsAny<Envio>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            await _controller.ActualizarEnvioApi(7, CrearSolicitudValida());

            _envioServiceMock.Verify(
                s => s.ActualizarEnvioAsync(7, It.IsAny<Envio>(), 1),
                Times.Once);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaRetornarNotFound_CuandoElEnvioNoExisteOEsDeOtroUsuario()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ActualizarEnvioAsync(999, It.IsAny<Envio>(), 1))
                .ReturnsAsync(false);

            var resultado = await _controller.ActualizarEnvioApi(999, CrearSolicitudValida());

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaRetornarBadRequest_CuandoElServicioRechazaLaEdicion()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ActualizarEnvioAsync(1, It.IsAny<Envio>(), 1))
                .ThrowsAsync(new InvalidOperationException("El envío solo puede editarse mientras busca conductor."));

            var resultado = await _controller.ActualizarEnvioApi(1, CrearSolicitudValida());

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal(400, badRequest.StatusCode);
            var detalle = Assert.IsType<ProblemDetails>(badRequest.Value);
            Assert.Equal("No se puede editar el envío.", detalle.Title);
            Assert.Equal("El envío solo puede editarse mientras busca conductor.", detalle.Detail);
        }

        [Fact]
        public async Task ActualizarEnvioApi_DeberiaRetornar500_CuandoOcurreUnErrorInesperado()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ActualizarEnvioAsync(1, It.IsAny<Envio>(), 1))
                .ThrowsAsync(new Exception("Fallo de base de datos"));

            var resultado = await _controller.ActualizarEnvioApi(1, CrearSolicitudValida());

            var objectResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Theory]
        [InlineData(EstadoEnvio.BuscandoConductor, true)]
        [InlineData(EstadoEnvio.ConductorAsignado, false)]
        [InlineData(EstadoEnvio.Cancelado, false)]
        public async Task CrearEnvio_DeberiaMapearPuedeEditarSegunElEstado(EstadoEnvio estado, bool esperado)
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.CrearEnvioAsync(It.IsAny<Envio>(), 1))
                .ReturnsAsync(new Envio
                {
                    Id = 1,
                    OrigenDir = "Mitre 1000",
                    DestinoDir = "Belgrano 500",
                    Estado = (byte)estado
                });

            var resultado = await _controller.CrearEnvio(CrearSolicitudValida());

            var created = Assert.IsType<CreatedAtActionResult>(resultado);
            var dto = Assert.IsType<EnvioDto>(created.Value);
            Assert.Equal(esperado, dto.PuedeEditar);
        }
    }
}