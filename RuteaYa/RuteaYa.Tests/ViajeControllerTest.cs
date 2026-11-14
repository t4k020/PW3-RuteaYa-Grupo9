using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Moq.Protected;
using RuteaYa.Controllers;
using RuteaYa.Entidades;
using RuteaYa.Models;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Dtos;
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
            var nuevoViaje = CrearViajeDtoValido();

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
            var nuevoViaje = CrearViajeDtoValido();

            // Act
            var resultado = await _controller.Crear(nuevoViaje);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<CrearViajeDto>(viewResult.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Crear_DeberiaMostrarDetalleDeValidacion_CuandoLaApiRespondeBadRequest()
        {
            // Arrange
            var detalles = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["OrigenDir"] = new[] { "El origen es obligatorio." }
            });
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(detalles)
            });
            var nuevoViaje = CrearViajeDtoValido();

            // Act
            var resultado = await _controller.Crear(nuevoViaje);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.IsType<CrearViajeDto>(viewResult.Model);
            Assert.Contains(
                _controller.ModelState[string.Empty]!.Errors,
                error => error.ErrorMessage.Contains("El origen es obligatorio."));
        }

        [Fact]
        public async Task Detalles_DeberiaRetornarVistaConViaje_CuandoLaApiRespondeOk()
        {
            // Arrange
            var viajePrueba = new ViajeDetalleDto
            {
                Id = 1,
                OrigenDir = "Av. Corrientes 1234, CABA",
                DestinoDir = "Av. Santa Fe 4321, CABA",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                MaxPaquetes = 5,
                PesoMaximoKg = 50.0m,
                DesvioMaxKm = 2.5m,
                Activo = true,
                DiasSemana = new List<string> { "Lunes", "Miércoles", "Viernes" }
            };
            var respuestaHttp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(viajePrueba)
            };
            ConfigurarRespuestaHttp(respuestaHttp);

            // Act
            var resultado = await _controller.Detalle(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var viajeRetornado = Assert.IsType<ViajeDetalleDto>(viewResult.Model);
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


        [Fact]
        public async Task Activar_DeberiaRedireccionarAIndexConMensajeExito_CuandoLaApiRespondeOk()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NoContent));

            // Act
            var resultado = await _controller.Activar(1);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("¡Viaje activado con éxito!", _controller.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Activar_DeberiaRedireccionarAIndexConError_CuandoLaApiRetornaNotFound()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var resultado = await _controller.Activar(999);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("No se encontró el viaje a activar.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Paquete_DeberiaRetornarVistaConModelo_CuandoLaApiRespondeOk()
        {
            // Arrange
            var paquetePrueba = CrearPaqueteDetalleDto();
            var respuestaHttp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(paquetePrueba)
            };
            ConfigurarRespuestaHttp(respuestaHttp);

            // Act
            var resultado = await _controller.Paquete(5, 12);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            // Sin ViewName explícito: MVC resuelve Views/Viaje/Paquete.cshtml por convención.
            Assert.Null(viewResult.ViewName);
            var modeloRetornado = Assert.IsType<PaqueteDetalleViewModel>(viewResult.Model);
            Assert.Equal(12, modeloRetornado.Paquete.EnvioId);
            Assert.Equal("Bruno Remitente", modeloRetornado.Paquete.NombreRemitente);
            Assert.False(modeloRetornado.VolverAMisTransportes);
        }

        [Fact]
        public async Task Paquete_DeberiaVolverAMisTransportes_CuandoOrigenEsMisTransportes()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(CrearPaqueteDetalleDto())
            });

            // Act
            var resultado = await _controller.Paquete(
                5,
                12,
                OrigenNavegacionPaquete.MisTransportes);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<PaqueteDetalleViewModel>(viewResult.Model);
            Assert.True(modeloRetornado.VolverAMisTransportes);
        }

        [Fact]
        public async Task Paquete_DeberiaVolverADetalleViaje_CuandoOrigenEsDetalleViaje()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(CrearPaqueteDetalleDto())
            });

            // Act
            var resultado = await _controller.Paquete(
                5,
                12,
                OrigenNavegacionPaquete.DetalleViaje);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modeloRetornado = Assert.IsType<PaqueteDetalleViewModel>(viewResult.Model);
            Assert.False(modeloRetornado.VolverAMisTransportes);
        }

        [Fact]
        public async Task Paquete_DeberiaRedireccionarADetalleConError_CuandoLaApiRetornaNotFound()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var resultado = await _controller.Paquete(5, 99);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Detalle", redirectToActionResult.ActionName);
            Assert.Equal(5, redirectToActionResult.RouteValues!["id"]);
            Assert.Equal(
                "No se encontró el paquete asignado a este viaje.",
                _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Paquete_DeberiaRedireccionarAMisTransportesConError_CuandoOrigenEsMisTransportesYLaApiRetornaNotFound()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var resultado = await _controller.Paquete(
                5,
                99,
                OrigenNavegacionPaquete.MisTransportes);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("MisTransportes", redirectToActionResult.ActionName);
            Assert.Equal(
                "No se encontró el paquete asignado a este viaje.",
                _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Paquete_DeberiaRedireccionarADetalleConError_CuandoOcurreHttpRequestException()
        {
            // Arrange
            SetHttpServerError(new HttpRequestException("Error de red"));

            // Act
            var resultado = await _controller.Paquete(5, 12);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Detalle", redirectToActionResult.ActionName);
            Assert.Equal(5, redirectToActionResult.RouteValues!["id"]);
            Assert.Equal(
                "No se pudo conectar con el servidor de la API.",
                _controller.TempData["Error"]);
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

        private PaqueteDetalleDto CrearPaqueteDetalleDto()
        {
            return new PaqueteDetalleDto
            {
                EnvioId = 12,
                ViajeId = 5,
                Descripcion = "Caja de libros",
                Estado = (byte)EstadoEnvio.Retirado,
                EstadoNombre = "Retirado",
                FechaLimite = DateTime.Today.AddDays(1),
                Monto = 3200m,
                PesoKg = 4.5m,
                Tamano = 2,
                TamanoNombre = "Mediano",
                OrigenDir = "Origen del paquete",
                DestinoDir = "Destino del paquete",
                CreadoEn = DateTime.Today,
                NombreRemitente = "Bruno Remitente",
                FechaViaje = DateTime.Today,
                DesvioKm = 1.5m,
                TiempoExtraMin = 7,
                Ganancia = 3200m
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
                DiasSemana = new List<byte> { 1, 3, 5 },
                MaxPaquetes = 5,
                PesoMaxKg = 50.0m,
                DesvioMaxKm = 2.5m,
                Paradas = new List<ParadaViajeDto>
                {
                    new() { Direccion = "Av. Callao 100, CABA" }
                }
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