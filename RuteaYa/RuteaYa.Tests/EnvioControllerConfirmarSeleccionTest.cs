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
    // Pantalla 11 para los envíos elegidos en Oportunidades: resumen antes de reservar.
    public class EnvioControllerConfirmarSeleccionTest
    {
        private const string BaseUrl = "https://localhost:7187/";
        private static readonly DateTime Fecha = new DateTime(2030, 1, 7);

        private static StringContent Json(object o) =>
            new(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

        // Responde con las oportunidades de la API; el resto de las rutas devuelve 404.
        private static EnvioController CrearController(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => responder(req));

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(handler.Object) { BaseAddress = new Uri(BaseUrl) });

            return new EnvioController(factory.Object)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }

        private static OportunidadDto Oportunidad(int id, decimal monto = 1000, decimal peso = 1, decimal desvio = 1) =>
            new()
            {
                Id = id, Descripcion = "P" + id, OrigenDir = "A", DestinoDir = "B",
                Monto = monto, PesoKg = peso, DesvioKm = desvio, Compatibilidad = "Muy compatible"
            };

        private static OportunidadesResultadoDto Resultado(params OportunidadDto[] oportunidades) =>
            new()
            {
                ViajeId = 5, OrigenDir = "CABA", DestinoDir = "Lanús", FechaViaje = Fecha,
                CupoDisponible = 3, PesoDisponibleKg = 10m, DesvioDisponibleKm = 5m,
                Oportunidades = oportunidades.ToList()
            };

        private static HttpResponseMessage RespuestaApi(HttpRequestMessage req, OportunidadesResultadoDto resultado)
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/oportunidades", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = Json(resultado) };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ConfirmarSeleccion_SinIds_RedirigeAOportunidades()
        {
            var c = CrearController(req => RespuestaApi(req, Resultado(Oportunidad(1))));

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int>());

            Assert.Equal("Oportunidades", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.NotNull(c.TempData["Error"]);
        }

        [Fact]
        public async Task ConfirmarSeleccion_SeleccionValida_MuestraElResumenConLosTotales()
        {
            var c = CrearController(req => RespuestaApi(req, Resultado(
                Oportunidad(1, monto: 4000, peso: 2, desvio: 1),
                Oportunidad(2, monto: 5500, peso: 6, desvio: 2),
                Oportunidad(3, monto: 100, peso: 1, desvio: 1))));

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int> { 2, 1 }, "desvio");

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Optimizador/Confirmar.cshtml", view.ViewName);
            var vm = Assert.IsType<ConfirmarCombinacionViewModel>(view.Model);
            Assert.True(vm.DesdeOportunidades);
            Assert.Equal("desvio", vm.Orden);
            Assert.Equal(5, vm.ViajeId);
            Assert.Equal(Fecha, vm.Fecha);
            Assert.Equal(2, vm.Combinacion.Paquetes.Count);
            Assert.Equal(9500m, vm.Combinacion.GananciaTotal);
            Assert.Equal(8m, vm.Combinacion.PesoTotalKg);
            Assert.Equal(3m, vm.Combinacion.DesvioTotalKm);
            Assert.True(vm.Combinacion.EsValida);
        }

        [Fact]
        public async Task ConfirmarSeleccion_EnvioQueYaNoEstaDisponible_VuelveAOportunidadesConConflicto()
        {
            var c = CrearController(req => RespuestaApi(req, Resultado(Oportunidad(1))));

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int> { 1, 99 });

            Assert.Equal("Oportunidades", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Contains("Otro conductor", (string)c.TempData["Error"]!);
        }

        [Fact]
        public async Task ConfirmarSeleccion_SeleccionQueSuperaElPeso_VuelveAOportunidadesConError()
        {
            var c = CrearController(req => RespuestaApi(req, Resultado(
                Oportunidad(1, peso: 6),
                Oportunidad(2, peso: 6))));

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int> { 1, 2 });

            Assert.Equal("Oportunidades", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Contains("supera", (string)c.TempData["Error"]!);
        }

        [Fact]
        public async Task ConfirmarSeleccion_SeleccionQueSuperaElCupo_VuelveAOportunidadesConError()
        {
            var resultado = Resultado(Oportunidad(1), Oportunidad(2));
            resultado.CupoDisponible = 1;
            var c = CrearController(req => RespuestaApi(req, resultado));

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int> { 1, 2 });

            Assert.Equal("Oportunidades", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Contains("supera", (string)c.TempData["Error"]!);
        }

        [Fact]
        public async Task ConfirmarSeleccion_ApiDevuelve400_MuestraElDetalleEnError()
        {
            var c = CrearController(req => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = Json(new { detail = "La fecha seleccionada no coincide con los días programados para este viaje." })
            });

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int> { 1 });

            Assert.Equal("Oportunidades", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal(
                "La fecha seleccionada no coincide con los días programados para este viaje.",
                c.TempData["Error"]);
        }

        [Fact]
        public async Task ConfirmarSeleccion_ApiDevuelve404_MuestraErrorDeViaje()
        {
            var c = CrearController(req => new HttpResponseMessage(HttpStatusCode.NotFound));

            var result = await c.ConfirmarSeleccion(5, Fecha, new List<int> { 1 });

            Assert.Equal("Oportunidades", Assert.IsType<RedirectToActionResult>(result).ActionName);
            Assert.Equal("No se encontró el viaje seleccionado.", c.TempData["Error"]);
        }
    }
}
