namespace RuteaYa.Servicios.Servicios.Rutas;

/// <summary>
/// Indica que el proveedor de rutas no pudo responder correctamente.
/// Es un problema del servicio externo, no de los datos que envió el usuario,
/// por eso los controllers lo traducen a 502 y no a 400.
/// </summary>
public sealed class ProveedorRutasException : Exception
{
    public ProveedorRutasException(string mensaje)
        : base(mensaje)
    {
    }

    public ProveedorRutasException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
