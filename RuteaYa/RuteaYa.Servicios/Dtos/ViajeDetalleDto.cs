namespace RuteaYa.Servicios.Dtos
{
    public class ViajeDetalleDto
    {
        public int Id { get; set; }
        public string OrigenDir { get; set; } = string.Empty;
        public decimal OrigenLat { get; set; }
        public decimal OrigenLng { get; set; }
        public string DestinoDir { get; set; } = string.Empty;
        public decimal DestinoLat { get; set; }
        public decimal DestinoLng { get; set; }
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly HoraLlegada { get; set; }
        public bool Activo { get; set; }
        public int MaxPaquetes { get; set; }
        public decimal PesoMaximoKg { get; set; }
        public decimal DesvioMaxKm { get; set; }
        public decimal DistanciaKm { get; set; }
        public int DuracionMin { get; set; }
        public List<string> DiasSemana { get; set; } = new();
        public List<ParadaDto> Paradas { get; set; } = new();
        public List<PaqueteDto> PaquetesAsignados { get; set; } = new();
    }
}
