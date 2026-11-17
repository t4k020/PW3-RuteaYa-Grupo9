using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Models
{
    // Datos de la pantalla del optimizador: viajes para elegir, selección actual y resultado.
    public class OptimizadorViewModel
    {
        public List<ViajeListadoDto> Viajes { get; set; } = new();
        public int? ViajeId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;

        // Null hasta que el usuario elige viaje y fecha, o si la API no pudo calcular.
        public OptimizacionDto? Resultado { get; set; }
    }

    // Datos de la pantalla de confirmación antes de reservar.
    public class ConfirmarCombinacionViewModel
    {
        public int ViajeId { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public CombinacionDto Combinacion { get; set; } = new();

        // True cuando la selección viene de Oportunidades: "Volver" regresa a esa pantalla
        // (con el mismo orden) en lugar de ir al optimizador.
        public bool DesdeOportunidades { get; set; }
        public string? Orden { get; set; }
    }
}
