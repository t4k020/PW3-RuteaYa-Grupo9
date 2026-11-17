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
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Xunit;
using System.Text.Json;

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
            var viajeModificado = new CrearViajeDto
            {
                OrigenDir = "Av. Corrientes 1234, CABA",
                DestinoDir = "Av. Santa Fe 4321, CABA",
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                DiasSemana = new List<byte> { 1 },
                MaxPaquetes = 5,
                PesoMaxKg = 50m,
                DesvioMaxKm = 2.5m
            };

            // Act - El método POST se llama Actualizar en el controlador
            var resultado = await _controller.Actualizar(1, viajeModificado);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("¡Viaje actualizado con éxito!", _controller.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Editar_Post_DeberiaMostrarElMotivoDeLaApi_CuandoNoPuedeGeocodificar()
        {
            // Arrange: la API responde 400 (ProblemDetails) porque la dirección
            // no se pudo ubicar.
            var problema = new
            {
                title = "Datos inválidos.",
                detail = "No se pudo ubicar la dirección de origen: Calle Inexistente 0.",
                status = 400
            };

            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(problema)
            });

            // Act
            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(
                _controller.ModelState[string.Empty]!.Errors,
                error => error.ErrorMessage.Contains("No se pudo ubicar la dirección"));
        }

        [Fact]
        public async Task Editar_Post_DeberiaMostrarElMotivoDeLaApi_CuandoElProveedorDeRutasFalla()
        {
            // Arrange: la API responde 502 porque el proveedor de rutas no respondió.
            var problema = new
            {
                title = "Servicio de rutas no disponible.",
                detail = "El proveedor de rutas no respondió a tiempo.",
                status = 502
            };

            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = JsonContent.Create(problema)
            });

            // Act
            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            // Assert
            Assert.IsType<ViewResult>(resultado);
            Assert.Contains(
                _controller.ModelState[string.Empty]!.Errors,
                error => error.ErrorMessage.Contains("no respondió a tiempo"));
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
            // HttpClient entrega una respuesta nueva por cada request. El mock
            // devuelve el mismo objeto, así que se guardan el código y el
            // cuerpo para recrearlos: si no, la segunda llamada encuentra el
            // contenido ya consumido.
            var codigo = respuesta.StatusCode;
            var contenido = respuesta.Content?.ReadAsStringAsync()
                .GetAwaiter()
                .GetResult();

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => new HttpResponseMessage(codigo)
                {
                    Content = contenido is null
                        ? null
                        : new StringContent(contenido, System.Text.Encoding.UTF8, "application/json")
                });
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

        // ---------- Editar viaje: GET ----------

        [Fact]
        public async Task Editar_Get_DeberiaRetornarVistaConElDetalleDelViaje_CuandoLaApiRespondeOk()
        {
            var viaje = CrearViajeDetalleValido(3);
            viaje.DiasSemana = new List<string> { "Lun", "Mié" };
            viaje.DiasSemanaNumeros = new List<byte> { 1, 3 };
            viaje.Paradas = new List<ParadaDto>
            {
                new ParadaDto { Id = 1, Orden = 1, Direccion = "Chacarita" }
            };
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(viaje)
            });

            var resultado = await _controller.Editar(3);

            // El formulario de edición usa el mismo contrato que la creación:
            // las coordenadas y las distancias las calcula la API.
            var viewResult = Assert.IsType<ViewResult>(resultado);
            var modelo = Assert.IsType<CrearViajeDto>(viewResult.Model);
            Assert.Equal(viaje.OrigenDir, modelo.OrigenDir);
            Assert.Equal(viaje.DestinoDir, modelo.DestinoDir);
            Assert.Equal(viaje.HoraSalida, modelo.HoraSalida);
            Assert.Equal(viaje.HoraLlegada, modelo.HoraLlegada);
            Assert.Equal(viaje.MaxPaquetes, modelo.MaxPaquetes);
            Assert.Equal(viaje.PesoMaximoKg, modelo.PesoMaxKg);
            Assert.Equal(viaje.DesvioMaxKm, modelo.DesvioMaxKm);
            Assert.Equal(new byte[] { 1, 3 }, modelo.DiasSemana);
            var parada = Assert.Single(modelo.Paradas);
            Assert.Equal("Chacarita", parada.Direccion);
        }

        [Fact]
        public async Task Editar_Get_DeberiaRedireccionarAIndexConError_CuandoLaApiRetornaNotFound()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            var resultado = await _controller.Editar(999);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("No se encontró el viaje a editar.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Editar_Get_DeberiaRedireccionarAIndex_CuandoLaApiFalla()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var resultado = await _controller.Editar(1);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Editar_Get_DeberiaRedireccionarAIndexConError_CuandoOcurreHttpRequestException()
        {
            SetHttpServerError(new HttpRequestException("Error de red"));

            var resultado = await _controller.Editar(1);

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("No se pudo conectar con el servidor de la API.", _controller.TempData["Error"]);
        }

        // ---------- Editar viaje: POST (Actualizar) ----------
        // El controlador ahora recibe CrearViajeDto y lo envía directo a la API.
        // Estos tests reflejan el nuevo contrato (CrearViajeDto).

        [Fact]
        public async Task Editar_Post_DeberiaRedireccionarAIndexConMensajeExito_CuandoLaApiRespondeNoContent()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NoContent));

            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("¡Viaje actualizado con éxito!", _controller.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Editar_Post_DeberiaEnviarUnPutConElDtoMapeado()
        {
            string? cuerpo = null;
            HttpRequestMessage? solicitud = null;
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((r, _) =>
                {
                    solicitud = r;
                    cuerpo = r.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

            var modelo = CrearViajeDtoValido();
            modelo.OrigenDir = "  Av. Rivadavia 100  ";
            modelo.DestinoDir = "  Av. Belgrano 200  ";
            modelo.PesoMaxKg = 30.5m;

            await _controller.Actualizar(4, modelo);

            Assert.NotNull(solicitud);
            Assert.Equal(HttpMethod.Put, solicitud!.Method);
            Assert.Equal("/api/ApiViaje/4", solicitud.RequestUri!.AbsolutePath);

            var dtoRecibido = JsonSerializer.Deserialize<CrearViajeDto>(
                cuerpo!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(dtoRecibido);
            // El MVC no normaliza los datos: envía el DTO tal cual y la API
            // se encarga de recortar y geocodificar las direcciones.
            Assert.Equal("  Av. Rivadavia 100  ", dtoRecibido!.OrigenDir);
            Assert.Equal("  Av. Belgrano 200  ", dtoRecibido.DestinoDir);
            Assert.Equal(30.5m, dtoRecibido.PesoMaxKg);
            Assert.Equal(modelo.DesvioMaxKm, dtoRecibido.DesvioMaxKm);
            Assert.Equal(modelo.MaxPaquetes, dtoRecibido.MaxPaquetes);
            Assert.Equal(modelo.HoraSalida, dtoRecibido.HoraSalida);
            Assert.Equal(modelo.HoraLlegada, dtoRecibido.HoraLlegada);
            Assert.Single(dtoRecibido.Paradas);
            Assert.Equal("Av. Callao 100, CABA", dtoRecibido.Paradas[0].Direccion);
        }

        [Theory]
        [InlineData("OrigenDir")]
        [InlineData("DestinoDir")]
        [InlineData("HoraLlegadaIgual")]
        [InlineData("HoraLlegadaAnterior")]
        [InlineData("MaxPaquetes")]
        [InlineData("PesoMaxKg")]
        [InlineData("DesvioMaxKm")]
        public async Task Editar_Post_DeberiaRetornarLaVistaConError_CuandoLosDatosSonInvalidos(string caso)
        {
            var modelo = CrearViajeDtoValido();
            string campoConError;
            switch (caso)
            {
                case "OrigenDir":
                    modelo.OrigenDir = "   ";
                    campoConError = nameof(CrearViajeDto.OrigenDir);
                    break;
                case "DestinoDir":
                    modelo.DestinoDir = "";
                    campoConError = nameof(CrearViajeDto.DestinoDir);
                    break;
                case "HoraLlegadaIgual":
                    modelo.HoraLlegada = modelo.HoraSalida!.Value;
                    campoConError = nameof(CrearViajeDto.HoraLlegada);
                    break;
                case "HoraLlegadaAnterior":
                    modelo.HoraLlegada = modelo.HoraSalida!.Value.AddHours(-1);
                    campoConError = nameof(CrearViajeDto.HoraLlegada);
                    break;
                case "MaxPaquetes":
                    modelo.MaxPaquetes = 0;
                    campoConError = nameof(CrearViajeDto.MaxPaquetes);
                    break;
                case "PesoMaxKg":
                    modelo.PesoMaxKg = 0;
                    campoConError = nameof(CrearViajeDto.PesoMaxKg);
                    break;
                default:
                    modelo.DesvioMaxKm = -1;
                    campoConError = nameof(CrearViajeDto.DesvioMaxKm);
                    break;
            }

            // El model binding de MVC aplica las DataAnnotations e
            // IValidatableObject del DTO; en un test unitario hay que simularlo.
            ValidarComoElModelBinding(modelo);

            var resultado = await _controller.Actualizar(1, modelo);

            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.Same(modelo, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(campoConError));
            VerificarQueNoSeLlamoALaApi();
        }

        [Fact]
        public async Task Editar_Post_DeberiaAceptarDesvioCero()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NoContent));
            var modelo = CrearViajeDtoValido();
            modelo.DesvioMaxKm = 0;

            var resultado = await _controller.Actualizar(1, modelo);

            Assert.IsType<RedirectToActionResult>(resultado);
        }

        [Fact]
        public async Task Editar_Post_DeberiaRetornarLaVistaSinLlamarALaApi_CuandoElModelStateYaEsInvalido()
        {
            _controller.ModelState.AddModelError("HoraSalida", "Hora inválida.");

            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            Assert.IsType<ViewResult>(resultado);
            VerificarQueNoSeLlamoALaApi();
        }

        [Fact]
        public async Task Editar_Post_DeberiaRedireccionarAIndexConError_CuandoLaApiRetornaNotFound()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.NotFound));

            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            var redirect = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("No se encontró el viaje a actualizar.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Editar_Post_DeberiaRetornarVistaConError_CuandoLaApiRetornaUnauthorized()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            var modelo = CrearViajeDtoValido();

            var resultado = await _controller.Actualizar(1, modelo);

            var viewResult = Assert.IsType<ViewResult>(resultado);
            Assert.Same(modelo, viewResult.Model);
            Assert.Equal(
                "No tienes autorización para realizar esta acción.",
                ObtenerErrorGeneral());
        }

        [Fact]
        public async Task Editar_Post_DeberiaRetornarVistaConError_CuandoLaApiFallaConErrorInterno()
        {
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            Assert.IsType<ViewResult>(resultado);
            Assert.Equal(
                "Ocurrió un error inesperado al comunicarse con el servidor.",
                ObtenerErrorGeneral());
        }

        [Fact]
        public async Task Editar_Post_DeberiaRetornarVistaConError_CuandoOcurreHttpRequestException()
        {
            SetHttpServerError(new HttpRequestException("Error de red"));

            var resultado = await _controller.Actualizar(1, CrearViajeDtoValido());

            Assert.IsType<ViewResult>(resultado);
            Assert.Equal("No se pudo conectar con el servidor de la API.", ObtenerErrorGeneral());
        }

        [Fact]
        public async Task PrevisualizarRecorrido_DeberiaRetornarJson_CuandoLaApiCalculaLaRuta()
        {
            // Arrange
            var recorrido = new RecorridoViajeDto
            {
                Puntos = new List<PuntoRecorridoDto>
                {
                    new() { Orden = 0, Direccion = "Origen", Lat = -34.60m, Lng = -58.39m },
                    new() { Orden = 1, Direccion = "Destino", Lat = -34.59m, Lng = -58.40m }
                },
                DistanciaKm = 5m,
                DuracionMin = 15,
                Fuente = "Mock"
            };

            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(recorrido)
            });

            // Act
            var resultado = await _controller.PrevisualizarRecorrido(CrearViajeDtoValido());

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(resultado);
            var recorridoDevuelto = Assert.IsType<RecorridoViajeDto>(jsonResult.Value);
            Assert.Equal(2, recorridoDevuelto.Puntos.Count);
            Assert.Equal("Mock", recorridoDevuelto.Fuente);
        }

        [Fact]
        public async Task PrevisualizarRecorrido_DeberiaRetornarBadRequest_CuandoElModeloEsInvalido()
        {
            // Arrange
            _controller.ModelState.AddModelError("OrigenDir", "El origen es obligatorio.");

            // Act
            var resultado = await _controller.PrevisualizarRecorrido(new CrearViajeDto());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(resultado);
            var detalles = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
            Assert.True(detalles.Errors.ContainsKey("OrigenDir"));

            // No se debe llamar a la API si el formulario no es válido.
            VerificarQueNoSeLlamoALaApi();
        }

        [Fact]
        public async Task PrevisualizarRecorrido_DeberiaRetornarElErrorDeLaApi_CuandoElProveedorFalla()
        {
            // Arrange
            ConfigurarRespuestaHttp(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = JsonContent.Create(new ProblemDetails
                {
                    Detail = "El proveedor de rutas no respondió.",
                    Status = (int)HttpStatusCode.BadGateway
                })
            });

            // Act
            var resultado = await _controller.PrevisualizarRecorrido(CrearViajeDtoValido());

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(502, objectResult.StatusCode);

            var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Contains("proveedor de rutas", problem.Detail);
        }

        // ---------- Métodos auxiliares ----------

        private string? ObtenerErrorGeneral()
        {
            return _controller.ModelState[string.Empty]?.Errors.Single().ErrorMessage;
        }

        private static ViajeDetalleDto CrearViajeDetalleValido(int id)
        {
            return new ViajeDetalleDto
            {
                Id = id,
                OrigenDir = "Av. Corrientes 1234, CABA",
                OrigenLat = -34.603722m,
                OrigenLng = -58.381592m,
                DestinoDir = "Av. Santa Fe 4321, CABA",
                DestinoLat = -34.595555m,
                DestinoLng = -58.393333m,
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(10, 0),
                Activo = true,
                MaxPaquetes = 5,
                PesoMaximoKg = 50.0m,
                DesvioMaxKm = 2.5m,
                DistanciaKm = 15.0m,
                DuracionMin = 60
            };
        }

        // Simula lo que hace el model binding de MVC: valida el DTO con sus
        // DataAnnotations e IValidatableObject y vuelca los errores en el
        // ModelState del controlador.
        private void ValidarComoElModelBinding(CrearViajeDto modelo)
        {
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(
                modelo,
                new ValidationContext(modelo),
                resultados,
                validateAllProperties: true);

            foreach (var resultado in resultados)
            {
                var miembros = resultado.MemberNames.Any()
                    ? resultado.MemberNames
                    : new[] { string.Empty };

                foreach (var miembro in miembros)
                {
                    _controller.ModelState.AddModelError(
                        miembro,
                        resultado.ErrorMessage ?? string.Empty);
                }
            }
        }

        private void VerificarQueNoSeLlamoALaApi()
        {
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}