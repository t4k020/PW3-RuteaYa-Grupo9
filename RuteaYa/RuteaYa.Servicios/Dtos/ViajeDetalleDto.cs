namespace RuteaYa.Servicios.Dtos
{
    public class ViajeDetalleDto
    {
        public int Id { get; set; }
        public string OrigenDir { get; set; }
        public string DestinoDir { get; set; }
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly HoraLlegada { get; set; }
        public bool Activo { get; set; }
        public int MaxPaquetes { get; set; }
        public decimal PesoMaximoKg { get; set; }
        public decimal DesvioMaxKm { get; set; }
        public List<string> DiasSemana { get; set; }
        public List<PaqueteDto> PaquetesAsignados { get; set; } = new();
    }
}
