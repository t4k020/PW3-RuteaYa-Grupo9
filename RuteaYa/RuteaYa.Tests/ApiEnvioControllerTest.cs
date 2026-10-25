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
            var envio = new Envio(); //objeto vacio
            //  Simulamos un usuario autenticado para que pase la primera validación del controlador
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "UsuarioTest") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
            };

            // se fuerza un error en el modelState
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

            //  Simulamos un usuario autenticado para que pase la primera validación del controlador
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "UsuarioTest") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
            };

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
            //  Simulamos un usuario autenticado para que pase la primera validación del controlador
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "UsuarioTest") }, "TestAuth"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user }
            };

            // Simulamos un error inesperado de base de datos o sistema
            _envioServiceMock.Setup(s => s.CrearEnvioAsync(It.IsAny<Envio>()))
                .ThrowsAsync(new Exception("Error crítico de conexión a la BD"));

            // Procedimiento
            var resultado = await _controller.CrearEnvio(envio);

            // Verificacion
            var statusCodeResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}
