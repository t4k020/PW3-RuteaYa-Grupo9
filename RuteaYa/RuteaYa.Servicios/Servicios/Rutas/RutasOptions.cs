namespace RuteaYa.Servicios.Servicios.Rutas;

// Proveedor de rutas habilitado por configuración.
public enum ProveedorRutas
{
    Mock,
    TomTom
}

/// <summary>
/// Configuración de la sección "Rutas". La API key nunca se versiona:
/// se carga con dotnet user-secrets o una variable de entorno.
/// </summary>
public sealed class RutasOptions
{
    public const string Seccion = "Rutas";

    public ProveedorRutas Proveedor { get; set; } = ProveedorRutas.Mock;

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.tomtom.com";

    // País usado para acotar la búsqueda de direcciones.
    public string PaisBusqueda { get; set; } = "AR";

    public int TimeoutSegundos { get; set; } = 10;

    public MockRutasOptions Mock { get; set; } = new();
}

/// <summary>
/// Parámetros del cálculo ficticio: permiten ajustar las distancias y tiempos
/// de desarrollo sin tocar el código del servicio.
/// </summary>
public sealed class MockRutasOptions
{
    // La distancia en línea recta se corrige por este factor para aproximar
    // el trazado real de las calles.
    public double FactorSinuosidad { get; set; } = 1.3;

    public double VelocidadKmH { get; set; } = 30;
}
