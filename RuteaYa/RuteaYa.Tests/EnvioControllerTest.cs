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
        public async Task Index_DeberiaRetornarVistaConListaDeEnvios_CuandoLaApiRespondeOk()
        {
            // Arrange
            var listaEnvios = new List<Envio>
            {
                new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" },
                new Envio { Id = 2, OrigenDir = "San Martin 200", DestinoDir = "Rivadavia 800" }
            };

            var respuestaHttp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(listaEnvios)
            };
            ConfigurarRespuestaHttp(respuestaHttp);

            // Act
            var resultado = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<List<Envio>>(viewResult.Model);
            Assert.Equal(2, modeloRetornado.Count);
            Assert.Equal("Mitre 1000", modeloRetornado[0].OrigenDir);
        }

        [Fact]
        public async Task Index_DeberiaRetornarVistaConListaVaciaYError_CuandoOcurreHttpRequestException()
        {
            // Arrange
            SetHttpServerError(new HttpRequestException("Error de red"));

            // Act
            var resultado = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<List<Envio>>(viewResult.Model);
            Assert.Empty(modeloRetornado);
            Assert.Equal("No se pudo conectar con el servidor de la API.", _controller.TempData["Error"]);
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

            ConfigurarRespuestaHttp(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });

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

            ConfigurarRespuestaHttp(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest });

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

            ConfigurarRespuestaHttp(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

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

            SetHttpServerError(new HttpRequestException("Fallo de red"));

            // Act
            var resultado = await _controller.Crear(envio);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty]!.Errors, e => e.ErrorMessage.Contains("No se pudo conectar"));
        }

        //testeo para getporid 
        [Fact]
        public async Task Detalles_DeberiaRetornarViewConEnvio_CuandoLaApiRespondeOk()
        {
            // Arrange
            var envioMock = new Envio { Id = 1, OrigenDir = "Mitre 1000", DestinoDir = "Belgrano 500" };
            var respuesta = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(envioMock)
            };
            ConfigurarRespuestaHttp(respuesta);

            // Act
            var resultado = await _controller.Detalles(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<Envio>(viewResult.Model);
            Assert.Equal(1, modeloRetornado.Id);
            Assert.Equal("Mitre 1000", modeloRetornado.OrigenDir);
        }

        [Fact]
        public async Task Detalles_DeberiaRedireccionarAIndex_CuandoLaApiRetornaNotFound()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var resultado = await _controller.Detalles(999);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No se encontró este envío.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Detalles_DeberiaRedireccionarAIndex_CuandoLaApiRetornaUnauthorized()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var resultado = await _controller.Detalles(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No tienes autorización para realizar esta acción.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Detalles_DeberiaRedireccionarAIndex_CuandoOcurreHttpRequestException()
        {
            // Arrange
            SetHttpServerError(new HttpRequestException("Error de red"));

            // Act
            var resultado = await _controller.Detalles(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No se pudo conectar con el servidor de la API.", _controller.TempData["Error"]);
        }

        //Tests para Eliminar envio

        [Fact]
        public async Task Eliminar_DeberiaRedireccionarAIndexConMensajeExito_CuandoLaApiRespondeOk()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NoContent)); 

            // Act
            var resultado = await _controller.Eliminar(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("¡Envío eliminado con éxito!", _controller.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Eliminar_DeberiaRedireccionarAIndexConError_CuandoLaApiRetornaNotFound()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var resultado = await _controller.Eliminar(999);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No se encontró el envío a eliminar.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Eliminar_DeberiaRedireccionarAIndexConError_CuandoLaApiRetornaUnauthorized()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var resultado = await _controller.Eliminar(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No tienes autorización para realizar esta acción.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Eliminar_DeberiaRedireccionarAIndexConError_CuandoOcurreHttpRequestException()
        {
            // Arrange
            SetHttpServerError(new HttpRequestException("Error de red"));

            // Act
            var resultado = await _controller.Eliminar(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No se pudo conectar con el servidor de la API.", _controller.TempData["Error"]);
        }

        private void ConfigurarRespuestaHttp(HttpResponseMessage respuesta)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(respuesta);
        }

        private void SetHttpServerError(Exception exception)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(exception);
        }
    }
}