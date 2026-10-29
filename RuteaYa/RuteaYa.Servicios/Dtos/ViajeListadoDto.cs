namespace RuteaYa.Servicios.Dtos
{
    public class ViajeListadoDto
    {
        public int Id { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly HoraLlegada { get; set; }
        public bool Activo { get; set; }
        public List<string> DiasSemana { get; set; } = new List<string>();
    }
}
