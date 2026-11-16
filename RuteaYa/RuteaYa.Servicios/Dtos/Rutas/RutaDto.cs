namespace RuteaYa.Servicios.Dtos;

// Resultado de calcular un recorrido entre dos o más puntos.
// La geometría es opcional: la implementación mock no la produce y la real
// la omite mientras se pida el resumen de la ruta.
public sealed record RutaDto(decimal DistanciaKm, int DuracionMin, string? Geometria);
