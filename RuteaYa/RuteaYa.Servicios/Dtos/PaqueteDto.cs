namespace RuteaYa.Servicios.Dtos
{
    public class PaqueteDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Estado { get; set; }
        // Nombre legible del estado, calculado en el servicio para no repetir la lógica en la vista.
        public string EstadoNombre { get; set; } = string.Empty;
        public DateTime FechaLimite { get; set; }
        public decimal Monto { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
    }
}
