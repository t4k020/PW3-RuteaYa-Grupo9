using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Moq.Protected;
using RuteaYa.Controllers;
using RuteaYa.Models;
using RuteaYa.Servicios.Dtos;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace RuteaYa.Tests
{
    public class OptimizadorControllerTest
    {
        private const string BaseUrl = "https://localhost:7187/";

        private static StringContent Json(object o) =>
            new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

        // Responde según la ruta pedida; el resto devuelve 404.
        private static OptimizadorController CrearController(
            Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => responder(req));

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(handler.Object) { BaseAddress = new Uri(BaseUrl) });

            return new OptimizadorController(factory.Object)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }

        private static PaqueteOptimizableDto Paquete(int id, decimal monto = 1000, decimal peso = 1) =>
            new() { Id = id, Descripcion = "P" + id, Monto = monto, PesoKg = peso, OrigenDir = "A", DestinoDir = "B" };

        private static OptimizacionDto Optimizacion()
        {
            var recomendada = new CombinacionDto { Paquetes = { Paquete(1), Paquete(2) }, GananciaTotal = 2000, EsValida = true };
            var alternativa = new CombinacionDto { Paquetes = { Paquete(3) }, GananciaTotal = 1000, EsValida = true };
            return new OptimizacionDto
            {
                ViajeId = 5, OrigenDir = "CABA", DestinoDir = "Lanús",
                FechaViaje = new DateTime(2030, 1, 7),
                Recomendada = recomendada,
                Alternativas = { alternativa }
            };
        }

        private static HttpResponseMessage RespuestaViajes() =>
            new(HttpStatusCode.OK) { Content = Json(new List<ViajeListadoDto>()) };

        [Fact]
        public async Task Index_SinSeleccion_NoCalculaYMuestraVista()
        {
            var optimizadorLlamado = false;
            var c = CrearController(req =>
            {
                if (req.RequestUri!.AbsolutePath.Contains("ApiOptimizador")) optimizadorLlamado = true;
                return RespuestaViajes();
            });

            var result = await c.Index();

            var vm = Assert.IsType<OptimizadorViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Null(vm.Resultado);
            Assert.False(optimizadorLlamado);
        }

        [Fact]
        public async Task Index_ConSeleccion_DevuelveResultado()
        {
            var c = CrearController(req =>
                req.RequestUri!.AbsolutePath.Contains("ApiOptimizador")
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = Json(Optimizacion()) }
                    : RespuestaViajes());

            var result = await c.Index(5, new DateTime(2030, 1, 7));

            var vm = Assert.IsType<OptimizadorViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.NotNull(vm.Resultado);
            Assert.Equal(2, vm.Resultado!.Recomendada!.Paquetes.Count);
        }

        [Fact]
        public async Task Index_ApiDevuelve400_MuestraDetalleEnError()
        {
            var c = CrearController(req =>
                req.RequestUri!.AbsolutePath.Contains("ApiOptimizador")
                    ? new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = Json(new { title = "No se puede optimizar el viaje.", detail = "No se puede optimizar para una fecha pasada." })
                    }
                    : RespuestaViajes());

            var result = await c.Index(5, new DateTime(2020, 1, 1));

            var vm = Assert.IsType<OptimizadorViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Null(vm.Resultado);
            Assert.Equal("No se puede optimizar para una fecha pasada.", c.TempData["Error"]);
        }

        [Fact]
        public async Task Index_ApiDevuelve404_MuestraErrorDeViaje()
        {
            var c = CrearController(req =>
                req.RequestUri!.AbsolutePath.Contains("ApiOptimizador")
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : RespuestaViajes());

            await c.Index(99, new DateTime(2030, 1, 7));

            Assert.Equal("No se encontró el viaje seleccionado.", c.TempData["Error"]);
        }

        [Fact]
        public async Task Confirmar_SinIds_RedirigeAIndex()
        {
            var c = CrearController(_ => RespuestaViajes());

            var result = await c.Confirmar(5, new DateTime(2030, 1, 7), new List<int>());

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.NotNull(c.TempData["Error"]);
        }

        [Fact]
        public async Task Confirmar_IdsDeLaRecomendada_MuestraResumen()
        {
            var c = CrearController(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = Json(Optimizacion()) });

            var result = await c.Confirmar(5, new DateTime(2030, 1, 7), new List<int> { 2, 1 });

            var vm = Assert.IsType<ConfirmarCombinacionViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(2000, vm.Combinacion.GananciaTotal);
            Assert.Equal("CABA", vm.OrigenDir);
        }

        [Fact]
        public async Task Confirmar_IdsDeUnaAlternativa_MuestraResumen()
        {
            var c = CrearController(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = Json(Optimizacion()) });

            var result = await c.Confirmar(5, new DateTime(2030, 1, 7), new List<int> { 3 });

            var vm = Assert.IsType<ConfirmarCombinacionViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(1000, vm.Combinacion.GananciaTotal);
        }

        [Fact]
        public async Task Confirmar_IdsQueYaNoCoinciden_RedirigeAIndexConError()
        {
            var c = CrearController(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = Json(Optimizacion()) });

            var result = await c.Confirmar(5, new DateTime(2030, 1, 7), new List<int> { 1, 99 });

            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Contains("Recalculá", c.TempData["Error"]!.ToString());
        }

        [Fact]
        public async Task Reservar_Exito_RedirigeAMisTransportes()
        {
            var c = CrearController(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

            var result = await c.Reservar(5, new DateTime(2030, 1, 7), new List<int> { 1, 2 });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MisTransportes", redirect.ActionName);
            Assert.Equal("Viaje", redirect.ControllerName);
            Assert.Equal("Reservaste 2 envíos.", c.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Reservar_UnSoloEnvio_UsaSingular()
        {
            var c = CrearController(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

            await c.Reservar(5, new DateTime(2030, 1, 7), new List<int> { 1 });

            Assert.Equal("Reservaste 1 envío.", c.TempData["Mensaje"]);
        }

        [Fact]
        public async Task Reservar_Conflicto400_VuelveAIndexConDetalle()
        {
            var c = CrearController(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = Json(new { title = "No se puede reservar la combinación.", detail = "Otro conductor tomó un envío o ya no está disponible. Recalculá la combinación." })
            });

            var result = await c.Reservar(5, new DateTime(2030, 1, 7), new List<int> { 1, 2 });

            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Contains("Otro conductor", c.TempData["Error"]!.ToString());
        }

        [Fact]
        public async Task Reservar_SinIds_NoLlamaALaApi()
        {
            var llamado = false;
            var c = CrearController(_ => { llamado = true; return new HttpResponseMessage(HttpStatusCode.NoContent); });

            var result = await c.Reservar(5, new DateTime(2030, 1, 7), new List<int>());

            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.False(llamado);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound, "No se encontró el viaje seleccionado.")]
        [InlineData(HttpStatusCode.Unauthorized, "No tienes autorización para realizar esta acción.")]
        [InlineData(HttpStatusCode.InternalServerError, "No se pudo reservar la combinación.")]
        public async Task Reservar_OtrosEstados_VuelveAIndexConMensaje(HttpStatusCode status, string esperado)
        {
            var c = CrearController(_ => new HttpResponseMessage(status));

            var result = await c.Reservar(5, new DateTime(2030, 1, 7), new List<int> { 1 });

            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(esperado, c.TempData["Error"]);
        }

        [Fact]
        public async Task Reservar_ApiCaida_MuestraErrorDeConexion()
        {
            var c = CrearController(_ => throw new HttpRequestException("sin conexión"));

            var result = await c.Reservar(5, new DateTime(2030, 1, 7), new List<int> { 1 });

            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal("No se pudo conectar con el servidor de la API.", c.TempData["Error"]);
        }
    }
}
