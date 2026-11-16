using Microsoft.Extensions.Options;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios.Rutas;
using Xunit;

namespace RuteaYa.Tests;

public class RutaServiceMockTest
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GeocodificarAsync_DeberiaResolverUnaDireccionConocida()
    {
        var servicio = CrearMock();

        var coordenada = await servicio.GeocodificarAsync("Av. Rivadavia 1234, CABA", Ct);

        Assert.NotNull(coordenada);
        Assert.Equal(-34.608344m, coordenada!.Lat);
        Assert.Equal(-58.392612m, coordenada.Lng);
    }

    [Fact]
    public async Task GeocodificarAsync_DeberiaIgnorarMayusculasAcentosYEspacios()
    {
        var servicio = CrearMock();

        var exacta = await servicio.GeocodificarAsync("Av. Rivadavia 1234, CABA", Ct);
        var desprolija = await servicio.GeocodificarAsync("  AV.   RIVADAVIA 1234, caba ", Ct);

        Assert.Equal(exacta, desprolija);
    }

    [Fact]
    public async Task GeocodificarAsync_DeberiaSerDeterministicoConDireccionesDesconocidas()
    {
        var servicio = CrearMock();
        const string direccion = "Pasaje Inventado 742, Villa Fiorito";

        var primera = await servicio.GeocodificarAsync(direccion, Ct);
        var segunda = await servicio.GeocodificarAsync(direccion, Ct);

        Assert.NotNull(primera);
        Assert.Equal(primera, segunda);
    }

    [Fact]
    public async Task GeocodificarAsync_DeberiaDevolverNull_CuandoLaDireccionNoTieneTexto()
    {
        var servicio = CrearMock();

        Assert.Null(await servicio.GeocodificarAsync("   ", Ct));
        Assert.Null(await servicio.GeocodificarAsync("1234", Ct));
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaDevolverDistanciaYDuracionPositivas()
    {
        var servicio = CrearMock();

        var ruta = await servicio.CalcularRutaAsync(new[]
        {
            new CoordenadaDto(-34.608344m, -58.392612m),
            new CoordenadaDto(-34.562700m, -58.459000m)
        }, Ct);

        Assert.True(ruta.DistanciaKm > 0);
        Assert.True(ruta.DuracionMin >= 1);
        Assert.NotNull(ruta.Geometria);
        Assert.True(ruta.Geometria!.Count > 2);
        Assert.Equal(new CoordenadaDto(-34.608344m, -58.392612m), ruta.Geometria[0]);
        Assert.Equal(new CoordenadaDto(-34.562700m, -58.459000m), ruta.Geometria[^1]);
        Assert.Equal("Mock", servicio.NombreProveedor);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaPasarPorLasParadasIntermedias()
    {
        var servicio = CrearMock();

        var origen = new CoordenadaDto(-34.608300m, -58.371200m);
        var parada = new CoordenadaDto(-34.562700m, -58.459000m);
        var destino = new CoordenadaDto(-34.626540m, -58.426280m);

        var ruta = await servicio.CalcularRutaAsync(new[] { origen, parada, destino }, Ct);

        Assert.NotNull(ruta.Geometria);
        Assert.Contains(parada, ruta.Geometria!);
        Assert.Equal(origen, ruta.Geometria![0]);
        Assert.Equal(destino, ruta.Geometria[^1]);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaDevolverLaMismaGeometriaSiempre()
    {
        var servicio = CrearMock();
        var puntos = new[]
        {
            new CoordenadaDto(-34.608344m, -58.392612m),
            new CoordenadaDto(-34.562700m, -58.459000m)
        };

        var primera = await servicio.CalcularRutaAsync(puntos, Ct);
        var segunda = await servicio.CalcularRutaAsync(puntos, Ct);

        Assert.Equal(primera.Geometria, segunda.Geometria);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaSerSimetrica()
    {
        var servicio = CrearMock();
        var origen = new CoordenadaDto(-34.608344m, -58.392612m);
        var destino = new CoordenadaDto(-34.562700m, -58.459000m);

        var ida = await servicio.CalcularRutaAsync(new[] { origen, destino }, Ct);
        var vuelta = await servicio.CalcularRutaAsync(new[] { destino, origen }, Ct);

        Assert.Equal(ida.DistanciaKm, vuelta.DistanciaKm);
        Assert.Equal(ida.DuracionMin, vuelta.DuracionMin);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaSerMasLarga_CuandoSeAgregaUnaParadaFueraDeLaRecta()
    {
        var servicio = CrearMock();

        var origen = new CoordenadaDto(-34.608300m, -58.371200m);  // Plaza de Mayo
        var parada = new CoordenadaDto(-34.562700m, -58.459000m);  // Cabildo 2000
        var destino = new CoordenadaDto(-34.626540m, -58.426280m); // Av. La Plata 1500

        var directa = await servicio.CalcularRutaAsync(new[] { origen, destino }, Ct);
        var conParada = await servicio.CalcularRutaAsync(new[] { origen, parada, destino }, Ct);

        Assert.True(conParada.DistanciaKm > directa.DistanciaKm);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaAplicarElFactorDeSinuosidadConfigurado()
    {
        var puntos = new[]
        {
            new CoordenadaDto(-34.608344m, -58.392612m),
            new CoordenadaDto(-34.562700m, -58.459000m)
        };

        var recta = await CrearMock(new RutasOptions
        {
            Mock = new MockRutasOptions { FactorSinuosidad = 1.0 }
        }).CalcularRutaAsync(puntos, Ct);

        var sinuosa = await CrearMock(new RutasOptions
        {
            Mock = new MockRutasOptions { FactorSinuosidad = 1.5 }
        }).CalcularRutaAsync(puntos, Ct);

        Assert.True(sinuosa.DistanciaKm > recta.DistanciaKm);
    }

    [Fact]
    public async Task CalcularRutaAsync_DeberiaFallar_ConMenosDeDosPuntos()
    {
        var servicio = CrearMock();

        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CalcularRutaAsync(Array.Empty<CoordenadaDto>(), Ct));

        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CalcularRutaAsync(new[] { new CoordenadaDto(-34.6m, -58.4m) }, Ct));
    }

    private static RutaServiceMock CrearMock(RutasOptions? opciones = null) =>
        new(Options.Create(opciones ?? new RutasOptions()));
}
