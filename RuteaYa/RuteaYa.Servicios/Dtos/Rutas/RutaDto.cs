namespace RuteaYa.Servicios.Dtos;

// Resultado de calcular un recorrido entre dos o más puntos.
// La geometría es opcional: el proveedor puede devolver solo el resumen y en
// ese caso el mapa dibuja los puntos del recorrido sin trazado intermedio.
public sealed record RutaDto(
    decimal DistanciaKm,
    int DuracionMin,
    IReadOnlyList<CoordenadaDto>? Geometria);
