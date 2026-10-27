using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Controllers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace RuteaYa.Tests
{
    public class ApiViajeControllerTest
    {

        private readonly Mock<IViajeService> _viajeServiceMock;
        private readonly ApiViajeController _controller;

        public ApiViajeControllerTest()
        {
            _viajeServiceMock = new Mock<IViajeService>();
            _controller = new ApiViajeController(_viajeServiceMock.Object);
        }



        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            // Act
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarOkConViaje_CuandoElViajeExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var viajePrueba = CrearViajeValido(1);

            _viajeServiceMock
                .Setup(s => s.ObtenerPorIdAsync(1))
                .ReturnsAsync(viajePrueba);

            // Act
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var viajeRetornado = Assert.IsType<Viaje>(okResult.Value);
            Assert.Equal(1, viajeRetornado.Id);
            Assert.Equal("Av. Corrientes 1234, CABA", viajeRetornado.OrigenDir);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarNotFound_CuandoElViajeNoExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.ObtenerPorIdAsync(999))
                .ReturnsAsync((Viaje?)null);

            // Act
            var resultado = await _controller.ObtenerPorId(999);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Crear_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcionEnElServicio()
        {
            // Arrange
            ConfigurarUsuarioAutenticado(); // <-- Agregamos esto para que pase la seguridad
            var viajeValido = new Viaje { Id = 1, OrigenDir = "Test" };

            // Simulamos que el servicio lanza una excepción no controlada
            _viajeServiceMock
                .Setup(s => s.CrearViajeAsync(It.IsAny<Viaje>()))
                .ThrowsAsync(new System.Exception("Error de base de datos"));

            // Act
            var resultado = await _controller.Crear(viajeValido);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Crear_DeberiaRetornarBadRequest_CuandoElModelStateEsInvalido()
        {
            // Arrange
            ConfigurarUsuarioAutenticado(); 
            _controller.ModelState.AddModelError("OrigenDir", "El campo es requerido");
            var viajeInvalido = new Viaje(); // Objeto incompleto/inválido

            // Act
            var resultado = await _controller.Crear(viajeInvalido);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task Crear_DeberiaRetornarCreatedAtAction_CuandoDatosSonValidos()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
        var nuevoViaje = CrearViajeValido(0);
        var viajeCreado = CrearViajeValido(1);

        _viajeServiceMock
            .Setup(s => s.CrearViajeAsync(It.IsAny<Viaje>()))
                .ReturnsAsync(viajeCreado);

        // Act
        var resultado = await _controller.Crear(nuevoViaje);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(nameof(ApiViajeController.ObtenerPorId), createdAtActionResult.ActionName);
            var viajeRetornado = Assert.IsType<Viaje>(createdAtActionResult.Value);
        Assert.Equal(1, viajeRetornado.Id);
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarNoContent_CuandoSeActualizaCorrectamente()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var viajeModificado = CrearViajeValido(1);

            _viajeServiceMock
                .Setup(s => s.ActualizarViajeAsync(1, viajeModificado))
                .ReturnsAsync(true);

            // Act
            var resultado = await _controller.Actualizar(1, viajeModificado);

            // Assert
            Assert.IsType<NoContentResult>(resultado); // Código 204
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarNotFound_CuandoElViajeNoExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var viajeModificado = CrearViajeValido(999);

            _viajeServiceMock
                .Setup(s => s.ActualizarViajeAsync(999, viajeModificado))
                .ReturnsAsync(false);

            // Act
            var resultado = await _controller.Actualizar(999, viajeModificado);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            // Act
            var resultado = await _controller.Actualizar(1, CrearViajeValido(1));

            // Assert
            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarBadRequest_CuandoElModelStateEsInvalido()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            _controller.ModelState.AddModelError("OrigenDir", "El campo es requerido");
            var viajeModificado = CrearViajeValido(1);

            // Act
            var resultado = await _controller.Actualizar(1, viajeModificado);

            // Assert
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var viajeModificado = CrearViajeValido(1);

            _viajeServiceMock
                .Setup(s => s.ActualizarViajeAsync(1, It.IsAny<Viaje>()))
                .ThrowsAsync(new System.Exception("Error de base de datos"));

            // Act
            var resultado = await _controller.Actualizar(1, viajeModificado);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Eliminar_DeberiaRetornarNoContent_CuandoElViajeExisteYSeElimina()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.EliminarViajeAsync(1))
                .ReturnsAsync(true);

            // Act
            var resultado = await _controller.Eliminar(1);

            // Assert
            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task Eliminar_DeberiaRetornarNotFound_CuandoElViajeNoExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.EliminarViajeAsync(999))
                .ReturnsAsync(false);

            // Act
            var resultado = await _controller.Eliminar(999);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Eliminar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            // Act
            var resultado = await _controller.Eliminar(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task Eliminar_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.EliminarViajeAsync(1))
                .ThrowsAsync(new System.Exception("Error de base de datos"));

            // Act
            var resultado = await _controller.Eliminar(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.ObtenerPorIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error crítico de base de datos"));

            // Act
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        private void ConfigurarUsuarioAutenticado()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "UsuarioTest")
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private Viaje CrearViajeValido(int id = 1)
        {
            return new Viaje
            {
                Id = id,
                UsuarioId = 1,
                OrigenDir = "Av. Corrientes 1234, CABA",
                OrigenLat = -34.603722m,
                OrigenLng = -58.381592m,
                DestinoDir = "Av. Santa Fe 4321, CABA",
                DestinoLat = -34.595555m,
                DestinoLng = -58.393333m,
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                MaxPaquetes = 5,
                PesoMaxKg = 50.0m,
                DesvioMaxKm = 2.5m,
                DistanciaKm = 15.0m,
                DuracionMin = 60,
                Activo = true
            };
        }
    }
}
