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
    public class ViajeControllerTest
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly ViajeController _controller;

        public ViajeControllerTest()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://localhost:7187/")
            };

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClientFactoryMock
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            _controller = new ViajeController(_httpClientFactoryMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                },
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }


        [Fact]
        public async Task Crear_DeberiaRedireccionarAIndexConMensajeExito_CuandoLaApiRespondeOk()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.Created));
            var nuevoViaje = CrearViajeValido(0);

            // Act
            var resultado = await _controller.Crear(nuevoViaje);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("¡Viaje creado con éxito!", _controller.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Crear_DeberiaRetornarVistaConModelo_CuandoOcurreHttpRequestException()
        {
            // Arrange
            SetHttpServerError(new HttpRequestException("Error de red"));
            var nuevoViaje = CrearViajeValido(0);

            // Act
            var resultado = await _controller.Crear(nuevoViaje);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<Viaje>(viewResult.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Detalles_DeberiaRetornarVistaConViaje_CuandoLaApiRespondeOk()
        {
            // Arrange
            var viajePrueba = CrearViajeValido(1);
            var respuestaHttp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(viajePrueba)
            };
            ConfigurarRespuestaHttp(respuestaHttp);

            // Act
            var resultado = await _controller.Detalle(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var viajeRetornado = Assert.IsType<Viaje>(viewResult.Model);
            Assert.Equal(1, viajeRetornado.Id);
            Assert.Equal("Av. Corrientes 1234, CABA", viajeRetornado.OrigenDir);
        }

        [Fact]
        public async Task Detalles_DeberiaRedireccionarAIndexConError_CuandoLaApiRetornaNotFound()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var resultado = await _controller.Detalle(999);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No se encontró este viaje.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Editar_Post_DeberiaRedireccionarAIndexConMensajeExito_CuandoLaApiRespondeOk()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NoContent));
            var viajeModificado = CrearViajeValido(1);

            // Act
            var resultado = await _controller.Editar(1, viajeModificado);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("¡Viaje actualizado con éxito!", _controller.TempData["Mensaje"]);
        }

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
            Assert.Equal("¡Viaje eliminado con éxito!", _controller.TempData["Mensaje"]);
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
            Assert.Equal("No se encontró el viaje a eliminar.", _controller.TempData["Error"]);
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

        private void SetHttpServerError(Exception excepcion)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(excepcion);
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