using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Servicios.Rutas;

/// <summary>
/// Abstracción del proveedor de rutas y mapas.
/// La lógica de negocio nunca depende de la implementación concreta.
/// </summary>
public interface IRutaService
{
    /// <summary>
    /// Nombre del proveedor activo, para mostrar la atribución de los datos
    /// sin exponer la configuración ni la API key.
    /// </summary>
    string NombreProveedor { get; }

    /// <summary>
    /// Convierte una dirección en coordenadas.
    /// Devuelve <c>null</c> cuando el proveedor no encuentra la dirección.
    /// </summary>
    Task<CoordenadaDto?> GeocodificarAsync(
        string direccion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula el recorrido que une los puntos en el orden recibido.
    /// </summary>
    /// <exception cref="ArgumentException">Se reciben menos de dos puntos.</exception>
    /// <exception cref="ProveedorRutasException">El proveedor no respondió o devolvió datos inválidos.</exception>
    Task<RutaDto> CalcularRutaAsync(
        IReadOnlyList<CoordenadaDto> puntos,
        CancellationToken cancellationToken = default);
}
