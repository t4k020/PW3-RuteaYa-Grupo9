using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Moq.Protected;
using RuteaYa.Controllers;
using RuteaYa.Entidades;
using RuteaYa.Servicios;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Xunit;

namespace RuteaYa.Tests
{
    public class EnviosControllerTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly EnvioController _controller;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;


        public EnviosControllerTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            var client = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://localhost:7187/")
            };

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(client);

            _controller = new EnvioController(_httpClientFactoryMock.Object);

            // Inicializamos TempData  con ITempDataProvider
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );
        }

        [Fact]
        public async Task Index_DeberiaRetornarViewResult_CuandoUsuarioEstaAutenticado()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.Name, "Juan") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var resultado = _controller.Index();

            // Assert
            var viewResult = resultado.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().BeNullOrEmpty(); // Usa la vista Index por defecto
        }

        [Fact]
        public async Task DeberiaRedirigirAHomeIndexCuandoUsuarioNoEstaAutenticado()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Simula usuario NO autenticado
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Act
            var resultado = _controller.Index();

            // Assert
            var redirectResult = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
        }

        [Fact]
        public async Task CuandoSeCreaUnEnvioCorrectamenteRedirigeAlIndex()
        {
            // Arrange
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            // Simulamos que la API responde exitosamente con un código 201 Created
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = JsonContent.Create(envio)
                });

            // Act
            var resultado = await _controller.Crear(envio);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task CuandoLaApiDevuelveUnauthorizedAgregaErrorAlModelState()
        {
            // Arrange
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            // Act
            var resultado = await _controller.Crear(envio);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("autorización"));
        }

        [Fact]
        public async Task CuandoLaApiDevuelveBadRequestAgregaErrorAlModelState()
        {
            // Arrange
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            // Act
            var resultado = await _controller.Crear(envio);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("datos enviados"));
        }

        [Fact]
        public async Task CuandoLaApiDevuelveErrorInesperadoAgregaErrorAlModelState()
        {
            // Arrange
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            // Act
            var resultado = await _controller.Crear(envio);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("error inesperado"));
        }

        [Fact]
        public async Task CuandoOcurreUnErrorEnElServerApiSeDevuelveUnMensajeDeErrorAlModelState()
        {
            // Arrange
            var envio = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };

            // Simulamos que la llamada HTTP lanza una excepción de red
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Fallo de red"));

            // Act
            var resultado = await _controller.Crear(envio);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("No se pudo conectar"));
        }
    }
}