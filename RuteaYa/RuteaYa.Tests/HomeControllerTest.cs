using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RuteaYa.Controllers;
using RuteaYa.Servicios;
using System.Security.Claims;
using Xunit;

namespace RuteaYa.Tests
{
    public class HomeControllerTest
    {

        private readonly Mock<IViajeService> _viajeServiceMock;
        private readonly Mock<IEnvioService> _envioServiceMock;

        public HomeControllerTest()
        {
            _viajeServiceMock = new Mock<IViajeService>();
            _envioServiceMock = new Mock<IEnvioService>();
        }
        [Fact]
        public async Task DeberiaRetornarIndexCuandoSeLoPide()
        {
            // Arrange (Preparación)

            
            var controller = new HomeController(_viajeServiceMock.Object, _envioServiceMock.Object);

            // Act (Ejecución)

            var resultado =  controller.Index();

            // Assert (Verificación)
            // 1. Validamos que el resultado sea un ViewResult
            var viewResult = resultado.Should().BeOfType<ViewResult>().Subject;


        }

        [Fact]
        public async Task DeberiaRetornarVistaMainpageCuandoUsuarioEstaAutenticado()
        {
            // Arrange
            var controller = new HomeController(_viajeServiceMock.Object, _envioServiceMock.Object);

            // Simular usuario autenticado en el HttpContext
            var claims = new[] { new Claim(ClaimTypes.Name, "Juan") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContextMock = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock
            };

            // Act
            var resultado =  controller.Index();

            // Assert
            var viewResult = resultado.Should().BeOfType<ViewResult>().Subject;

            // Si retornas View("Mainpage", model) en el controller:
            viewResult.ViewName.Should().Be("Mainpage");
            
        }

        [Fact]
        public async Task DeberiaRedirigirALoginCuandoUsuarioNoEstaAutenticado()
        {
            // Arrange
            var controller = new HomeController(_viajeServiceMock.Object, _envioServiceMock.Object);

            // Simular usuario NO autenticado
            var identity = new ClaimsIdentity(); // Sin authenticationType indica no autenticado
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContextMock = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContextMock
            };

            // Act
            var resultado = controller.Index();

            // Assert
            var redirectResult = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Login"); // O "Index" según la acción de tu login
            redirectResult.ControllerName.Should().Be("Account"); // O "Main" según tu estructura
        }
    }
}
