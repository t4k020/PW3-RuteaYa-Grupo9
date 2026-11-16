using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios.Rutas;
using Xunit;

namespace RuteaYa.Tests;

/// <summary>
/// Tests del proveedor real. No salen a internet: el HttpClient se arma con
/// un handler que captura la petición y devuelve una respuesta fija.
/// </summary>
public class TomTomRutaServiceTest
{
    private const string ApiKeyDePrueba = "clave-de-prueba";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GeocodificarAsync_DeberiaArmarLaUrlConDireccionPaisYClave()
    {
        Uri? solicitada = null;
        var handler = new HandlerCaptura(peticion =>
        {
            solicitada = peticion.RequestUri;
            return Json("""
                { "results": [ { "position": { "lat": -34.6083, "lon": -58.3926 } } ] }
                """);
        });

        var coordenada = await CrearServicio(handler).GeocodificarAsync(
            "Av. Corrientes 1234, CABA", Ct);

        Assert.NotNull(coordenada);
        Assert.Equal(-34.61m, coordenada!.Lat);
        Assert.Equal(-58.39m, coordenada.Lng);

        var url = solicitada!.AbsoluteUri;
        Assert.Contains("search/2/geocode/", url);
        Assert.Contains("countrySet=AR", url);
        Assert.Contains($"key={ApiKeyDePrueba}", url);
        Assert.Contains(Uri.EscapeDataString("Av. Corrientes 1234, CABA"), url);
    }

    [Fact]
    public async Task GeocodificarAsync_DeberiaDevolverNull_CuandoNoHayResultados()
    {
        var handler = new HandlerCaptura(_ => Json("""{ "results": [] }"""));

        Assert.Null(await CrearServicio(handler).GeocodificarAsync("Calle Inexistente 0", Ct));
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaArmarElRecorridoYSoloPedirElResumen()
    {
        Uri? solicitada = null;
        var handler = new HandlerCaptura(peticion =>
        {
            solicitada = peticion.RequestUri;
            return Json("""
                { "routes": [ { "summary": { "lengthInMeters": 15000, "travelTimeInSeconds": 1800 } } ] }
                """);
        });

        var ruta = await CrearServicio(handler).CalcularRutaAsync(
            new[]
            {
                new CoordenadaDto(-34.60m, -58.39m),
                new CoordenadaDto(-34.56m, -58.45m)
            },
            Ct);

        Assert.Equal(15m, ruta.DistanciaKm);
        Assert.Equal(30, ruta.DuracionMin);
        Assert.Null(ruta.Geometria);

        var url = solicitada!.AbsoluteUri;
        Assert.Contains("calculateRoute/-34.60,-58.39:-34.56,-58.45/json", url);
        Assert.Contains("routeRepresentation=summaryOnly", url);
        Assert.Contains("traffic=false", url);
        Assert.Contains($"key={ApiKeyDePrueba}", url);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaFallar_CuandoElProveedorRespondeError()
    {
        var handler = new HandlerCaptura(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        var excepcion = await Assert.ThrowsAsync<ProveedorRutasException>(
            () => CrearServicio(handler).CalcularRutaAsync(
                new[]
                {
                    new CoordenadaDto(-34.60m, -58.39m),
                    new CoordenadaDto(-34.56m, -58.45m)
                },
                Ct));

        Assert.Contains("400", excepcion.Message);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaFallar_CuandoLaRespuestaNoEsJson()
    {
        var handler = new HandlerCaptura(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("no es json", Encoding.UTF8, "text/plain")
        });

        await Assert.ThrowsAsync<ProveedorRutasException>(
            () => CrearServicio(handler).CalcularRutaAsync(
                new[]
                {
                    new CoordenadaDto(-34.60m, -58.39m),
                    new CoordenadaDto(-34.56m, -58.45m)
                },
                Ct));
    }

    private static TomTomRutaService CrearServicio(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.tomtom.com/") },
            Options.Create(new RutasOptions
            {
                Proveedor = ProveedorRutas.TomTom,
                ApiKey = ApiKeyDePrueba,
                PaisBusqueda = "AR"
            }));

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class HandlerCaptura : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public HandlerCaptura(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
