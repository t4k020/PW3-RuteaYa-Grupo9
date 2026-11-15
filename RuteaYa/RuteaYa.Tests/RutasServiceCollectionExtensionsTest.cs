using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RuteaYa.Servicios.Servicios.Rutas;
using Xunit;

namespace RuteaYa.Tests;

public class RutasServiceCollectionExtensionsTest
{
    [Fact]
    public void AddRutas_DeberiaRegistrarElMock_CuandoNoHayConfiguracion()
    {
        using var proveedor = ConstruirProveedor(new Dictionary<string, string?>());

        Assert.IsType<RutaServiceMock>(proveedor.GetRequiredService<IRutaService>());
    }

    [Fact]
    public void AddRutas_DeberiaRegistrarElMock_CuandoTomTomNoTieneApiKey()
    {
        using var proveedor = ConstruirProveedor(new Dictionary<string, string?>
        {
            ["Rutas:Proveedor"] = "TomTom",
            ["Rutas:ApiKey"] = ""
        });

        Assert.IsType<RutaServiceMock>(proveedor.GetRequiredService<IRutaService>());
    }

    [Fact]
    public void AddRutas_DeberiaRegistrarTomTom_CuandoHayApiKey()
    {
        using var proveedor = ConstruirProveedor(new Dictionary<string, string?>
        {
            ["Rutas:Proveedor"] = "TomTom",
            ["Rutas:ApiKey"] = "clave-de-prueba"
        });

        Assert.IsType<TomTomRutaService>(proveedor.GetRequiredService<IRutaService>());
    }

    private static ServiceProvider ConstruirProveedor(Dictionary<string, string?> valores)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddRutas(configuracion);

        return servicios.BuildServiceProvider();
    }
}
