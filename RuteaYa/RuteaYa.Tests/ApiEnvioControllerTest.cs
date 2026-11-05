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
                .Setup(s => s.ObtenerPorIdAsync(1, 1))
                .ReturnsAsync(new Envio
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
                .Setup(s => s.ObtenerPorIdAsync(999, 1))
                .ReturnsAsync((Envio?)null);

            var resultado = await _controller.ObtenerPorIdApi(999);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPorIdApi_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            ConfigurarUsuarioAutenticado();
            _envioServiceMock
                .Setup(s => s.ObtenerPorIdAsync(It.IsAny<int>(), 1))
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
    }
}
