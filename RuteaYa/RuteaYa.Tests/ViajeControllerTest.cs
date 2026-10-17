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
    public class ViajesControllerTests
    {
        private readonly Mock<IViajeService> _viajeServiceMock;

        public ViajesControllerTests()
        {
            _viajeServiceMock = new Mock<IViajeService>();
        }

        [Fact]
        public async Task Index_DeberiaRetornarViewResult_CuandoUsuarioEstaAutenticado()
        {
            // Arrange
            var controller = new ViajeController(_viajeServiceMock.Object);

            var claims = new[] { new Claim(ClaimTypes.Name, "Juan") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var resultado =  controller.Index();

            // Assert
            var viewResult = resultado.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task Index_DeberiaRedirigirAHomeIndex_CuandoUsuarioNoEstaAutenticado()
        {
            // Arrange
            var controller = new ViajeController(_viajeServiceMock.Object);

            // Simular usuario NO autenticado
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Act
            var resultado =  controller.Index();

            // Assert
            var redirectResult = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
        }
    }
}