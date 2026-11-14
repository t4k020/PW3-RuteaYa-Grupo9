using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Entidades;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Dtos;
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
            ConfigurarUsuarioNoAutenticado();

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
            var viajePrueba = CrearDetalleValido(1);

            _viajeServiceMock
                .Setup(s => s.ObtenerDetallePorIdAsync(1, 1))
                .ReturnsAsync(viajePrueba);

            // Act
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var viajeRetornado = Assert.IsType<ViajeDetalleDto>(okResult.Value);
            Assert.Equal(1, viajeRetornado.Id);
            Assert.Equal("Av. Corrientes 1234, CABA", viajeRetornado.OrigenDir);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarNotFound_CuandoElViajeNoExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.ObtenerDetallePorIdAsync(999, 1))
                .ReturnsAsync((ViajeDetalleDto?)null);

            // Act
            var resultado = await _controller.ObtenerPorId(999);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Crear_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcionEnElServicio()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var solicitud = CrearViajeDtoValido();

            // Simulamos que el servicio lanza una excepción no controlada
            _viajeServiceMock
                .Setup(s => s.CrearViajeAsync(It.IsAny<Viaje>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

            // Act
            var resultado = await _controller.Crear(solicitud);

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
            var solicitudInvalida = new CrearViajeDto();

            // Act
            var resultado = await _controller.Crear(solicitudInvalida);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.Equal(400, badRequestResult.StatusCode);
            var detalles = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
            Assert.True(detalles.Errors.ContainsKey("OrigenDir"));
        }

        [Fact]
        public async Task Crear_DeberiaMapearElDtoYRetornarCreatedAtAction_CuandoDatosSonValidos()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var solicitud = CrearViajeDtoValido();
            var viajeCreado = CrearViajeValido(1);
            Viaje? viajeMapeado = null;

            _viajeServiceMock
                .Setup(s => s.CrearViajeAsync(It.IsAny<Viaje>()))
                .Callback<Viaje>(viaje => viajeMapeado = viaje)
                .ReturnsAsync(viajeCreado);

            _viajeServiceMock
                .Setup(s => s.ObtenerDetallePorIdAsync(1, 1))
                .ReturnsAsync(CrearDetalleValido(1));

            // Act
            var resultado = await _controller.Crear(solicitud);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(nameof(ApiViajeController.ObtenerPorId), createdAtActionResult.ActionName);

            var detalleRetornado = Assert.IsType<ViajeDetalleDto>(createdAtActionResult.Value);
            Assert.Equal(1, detalleRetornado.Id);

            Assert.NotNull(viajeMapeado);
            Assert.Equal(1, viajeMapeado.UsuarioId);
            Assert.True(viajeMapeado.Activo);
            Assert.Equal(0, viajeMapeado.OrigenLat);
            Assert.Equal(0, viajeMapeado.OrigenLng);
            Assert.Equal(new[] { 1, 2 }, viajeMapeado.Paradas.OrderBy(p => p.Orden).Select(p => p.Orden));
            Assert.Equal("Parada 1", viajeMapeado.Paradas.OrderBy(p => p.Orden).First().Direccion);
            Assert.Equal(new byte[] { 1, 3 }, viajeMapeado.ViajeDias.OrderBy(d => d.DiaSemana).Select(d => d.DiaSemana));
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarNoContent_CuandoSeActualizaCorrectamente()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var viajeModificado = CrearViajeValido(1);

            _viajeServiceMock
                .Setup(s => s.ActualizarViajeAsync(1, viajeModificado, 1))
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
                .Setup(s => s.ActualizarViajeAsync(999, viajeModificado, 1))
                .ReturnsAsync(false);

            // Act
            var resultado = await _controller.Actualizar(999, viajeModificado);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Actualizar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            ConfigurarUsuarioNoAutenticado();

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
                .Setup(s => s.ActualizarViajeAsync(1, It.IsAny<Viaje>(), 1))
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
                .Setup(s => s.EliminarViajeAsync(1, 1))
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
                .Setup(s => s.EliminarViajeAsync(999, 1))
                .ReturnsAsync(false);

            // Act
            var resultado = await _controller.Eliminar(999);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Eliminar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            ConfigurarUsuarioNoAutenticado();

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
                .Setup(s => s.EliminarViajeAsync(1, 1))
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
                .Setup(s => s.ObtenerDetallePorIdAsync(It.IsAny<int>(), 1))
                .ThrowsAsync(new Exception("Error crítico de base de datos"));

            // Act
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Activar_DeberiaRetornarNoContent_CuandoElViajeExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.ActivarViajeAsync(1, 1))
                .ReturnsAsync(true);

            // Act
            var resultado = await _controller.Activar(1);

            // Assert
            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task Activar_DeberiaRetornarNotFound_CuandoElViajeNoExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _viajeServiceMock
                .Setup(s => s.ActivarViajeAsync(999, 1))
                .ReturnsAsync(false);

            // Act
            var resultado = await _controller.Activar(999);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Activar_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            ConfigurarUsuarioNoAutenticado();

            // Act
            var resultado = await _controller.Activar(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(resultado);
        }

        private void ConfigurarUsuarioAutenticado()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private void ConfigurarUsuarioNoAutenticado()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
        }

        private CrearViajeDto CrearViajeDtoValido()
        {
            return new CrearViajeDto
            {
                OrigenDir = "Av. Corrientes 1234, CABA",
                DestinoDir = "Av. Santa Fe 4321, CABA",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                DiasSemana = new List<byte> { 3, 1 },
                MaxPaquetes = 5,
                PesoMaxKg = 50.0m,
                DesvioMaxKm = 2.5m,
                Paradas = new List<ParadaViajeDto>
                {
                    new() { Direccion = "Parada 1" },
                    new() { Direccion = "Parada 2" }
                }
            };
        }

        private ViajeDetalleDto CrearDetalleValido(int id = 1)
        {
            return new ViajeDetalleDto
            {
                Id = id,
                OrigenDir = "Av. Corrientes 1234, CABA",
                DestinoDir = "Av. Santa Fe 4321, CABA",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                Activo = true,
                MaxPaquetes = 5,
                PesoMaximoKg = 50.0m,
                DesvioMaxKm = 2.5m,
                DiasSemana = new List<string>()
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
