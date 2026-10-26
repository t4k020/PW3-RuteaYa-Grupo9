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
    public class ApiEnvioControllerTest
    {
        private readonly Mock<IEnvioService> _envioServiceMock;
        private readonly ApiEnvioController _controller;



        public ApiEnvioControllerTest()
        {
            //mock
            _envioServiceMock = new Mock<IEnvioService>();
            _controller = new ApiEnvioController(_envioServiceMock.Object);

            // Inicializamos un contexto HTTP por defecto para evitar NullReferenceException
            // al acceder a User o HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
        }



        [Fact]
        public async Task CuandoSeCreaUnEnvioCorrectamentePeroElUsuarioNoTieneCredencialesSeRecibeUn401()
        {
            // Preparacion
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            // Simulamos que el usuario no está autorizado (puedes ajustar el controlador para validar esto con [Authorize]).
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() // Sin usuario autenticado
            };

            // Procedimiento
            var resultado = await _controller.CrearEnvio(envio);

            // Verificacion
            // Verificamos que devuelva 401 Unauthorized si no hay sesión/autorización
            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task CuandoSeCreaUnEnvioYSuModelStateEsInvalidoSeRecibeUn400()
        {
            // Preparacion
            var envio = new Envio(); // objeto vacio
            ConfigurarUsuarioAutenticado(); 

            // Se fuerza un error en el modelState
            _controller.ModelState.AddModelError("OrigenDir", "La dirección de origen es obligatoria.");

            // Procedimiento
            var resultado = await _controller.CrearEnvio(envio);

            // Verificacion
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CuandoSeCreaUnEnvioYElEstadoDelObjetoEsInvalidoSeRecibeUn400()
        {
            // Preparacion
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };
            ConfigurarUsuarioAutenticado(); 

            // Configuramos el mock para que lance la excepción de regla de negocio
            _envioServiceMock.Setup(s => s.CrearEnvioAsync(It.IsAny<Envio>()))
                .ThrowsAsync(new InvalidOperationException("El usuario asociado no existe."));

            // Procedimiento
            var resultado = await _controller.CrearEnvio(envio);

            // Verificacion
            Assert.IsType<BadRequestObjectResult>(resultado);
        }

        [Fact]
        public async Task CuandoSeCreaUnEnvioYHayUnErrorInternoSeRecibeUn500()
        {
            // Preparacion
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };
            ConfigurarUsuarioAutenticado(); // Reutilizamos el método auxiliar

            // Simulamos un error inesperado de base de datos o sistema
            _envioServiceMock.Setup(s => s.CrearEnvioAsync(It.IsAny<Envio>()))
                .ThrowsAsync(new Exception("Error crítico de conexión a la BD"));

            // Procedimiento
            var resultado = await _controller.CrearEnvio(envio);

            // Verificacion
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        //tests para get por id 

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarUnauthorized_CuandoUsuarioNoEstaAutenticado()
        {
            // Act (El HttpContext por defecto no tiene un usuario autenticado, por lo que IsAuthenticated será falso)
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            Assert.IsType<UnauthorizedResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarOkConEnvio_CuandoElEnvioExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();
            var envioMock = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            _envioServiceMock
                .Setup(s => s.ObtenerPorIdAsync(1))
                .ReturnsAsync(envioMock);

            // Act
            var resultado = await _controller.ObtenerPorId(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(resultado);
            var envioRetornado = Assert.IsType<Envio>(okResult.Value);
            Assert.Equal(1, envioRetornado.Id);
            Assert.Equal("Mitre 1000", envioRetornado.OrigenDir);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarNotFound_CuandoElEnvioNoExiste()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _envioServiceMock
                .Setup(s => s.ObtenerPorIdAsync(999))
                .ReturnsAsync((Envio?)null);

            // Act
            var resultado = await _controller.ObtenerPorId(999);

            // Assert
            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task ObtenerPorId_DeberiaRetornarStatusCode500_CuandoOcurreUnaExcepcion()
        {
            // Arrange
            ConfigurarUsuarioAutenticado();

            _envioServiceMock
                .Setup(s => s.ObtenerPorIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Error de base de datos"));

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
            new Claim(ClaimTypes.Name, "usuario_test")
        }, "mock"));

            _controller.ControllerContext.HttpContext.User = user;
        }
    }
}
