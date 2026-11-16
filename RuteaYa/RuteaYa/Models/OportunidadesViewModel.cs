using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Models;

public sealed class OportunidadesViewModel
{
    public List<ViajeListadoDto> Viajes { get; set; } = new();
    public int? ViajeId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Orden { get; set; } = "ganancia";
    public OportunidadesResultadoDto? Resultado { get; set; }
}
