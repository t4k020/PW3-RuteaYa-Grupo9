using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuteaYa.Servicios.Servicios.Rutas;

namespace RuteaYa.Servicios.Servicios.Rutas;

/// <summary>
/// Registro de la abstracción de rutas. Si no hay API key configurada se usa
/// el mock, así el repositorio recién clonado funciona sin configuración.
/// </summary>
public static class RutasServiceCollectionExtensions
{
    public static IServiceCollection AddRutas(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var seccion = configuration.GetSection(RutasOptions.Seccion);

        services.Configure<RutasOptions>(seccion);

        var opciones = seccion.Get<RutasOptions>() ?? new RutasOptions();

        if (opciones.Proveedor == ProveedorRutas.TomTom
            && !string.IsNullOrWhiteSpace(opciones.ApiKey))
        {
            services.AddHttpClient<IRutaService, TomTomRutaService>(cliente =>
            {
                cliente.BaseAddress = new Uri(opciones.BaseUrl);
                cliente.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
            });

            return services;
        }

        services.AddSingleton<IRutaService>(proveedor =>
        {
            if (opciones.Proveedor == ProveedorRutas.TomTom)
            {
                var logger = proveedor
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger<RutaServiceMock>();

                logger.LogWarning(
                    "Rutas:Proveedor está en TomTom pero falta Rutas:ApiKey. " +
                    "Se usa el servicio de rutas mock.");
            }

            return new RutaServiceMock(
                proveedor.GetRequiredService<IOptions<RutasOptions>>());
        });

        return services;
    }
}
