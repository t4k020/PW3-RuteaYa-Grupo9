namespace RuteaYa.Servicios.Dtos
{
    public class PaqueteDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public int Estado { get; set; }
        public DateTime FechaLimite { get; set; }
        public decimal Monto { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
    }
}
