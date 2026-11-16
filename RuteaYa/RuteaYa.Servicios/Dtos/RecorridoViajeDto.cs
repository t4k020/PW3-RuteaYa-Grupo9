namespace RuteaYa.Servicios.Dtos;

// Punto del recorrido que se muestra en el mapa, en el orden del viaje.
public sealed class PuntoRecorridoDto
{
    public int Orden { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}

// Recorrido calculado para dibujar el mapa de un viaje.
// La geometría se calcula on-demand: no se persiste en la base.
public sealed class RecorridoViajeDto
{
    public int ViajeId { get; set; }
    public List<PuntoRecorridoDto> Puntos { get; set; } = new();
    public List<CoordenadaDto> Geometria { get; set; } = new();
    public decimal DistanciaKm { get; set; }
    public int DuracionMin { get; set; }

    // Proveedor que resolvió la ruta, para mostrar la atribución correcta.
    public string Fuente { get; set; } = string.Empty;
}
