namespace RuteaYa.Servicios.Dtos
{
    public class PaqueteDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public int Estado { get; set; }
        public DateTime FechaLimite { get; set; }
        public decimal Monto { get; set; }
    }
}
