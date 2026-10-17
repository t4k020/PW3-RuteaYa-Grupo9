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
    public class EnviosControllerTests
    {
        private readonly Mock<IEnvioService> _envioServiceMock;

        public EnviosControllerTests()
        {
            _envioServiceMock = new Mock<IEnvioService>();
        }

        [Fact]
        public async Task Index_DeberiaRetornarViewResult_CuandoUsuarioEstaAutenticado()
        {
            // Arrange
            var controller = new EnvioController(_envioServiceMock.Object);

            
            var claims = new[] { new Claim(ClaimTypes.Name, "Juan") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var resultado = controller.Index();

            // Assert
            var viewResult = resultado.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().BeNullOrEmpty(); // Usa la vista Index por defecto
        }

        [Fact]
        public async Task Index_DeberiaRedirigirAHomeIndex_CuandoUsuarioNoEstaAutenticado()
        {
            // Arrange
            var controller = new EnvioController(_envioServiceMock.Object);

            // Simula usuario NO autenticado
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Act
            var resultado = controller.Index();

            // Assert
            var redirectResult = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
        }
    }
}