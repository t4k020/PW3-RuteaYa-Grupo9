namespace RuteaYa.Servicios.Dtos
{
    // DTO de salida para el detalle de un paquete asignado a un viaje.
    // No incluye datos privados del remitente (email, credenciales, etc.).
    public sealed class PaqueteDetalleDto
    {
        public int EnvioId { get; set; }
        public int ViajeId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public DateTime FechaLimite { get; set; }
        public decimal Monto { get; set; }
        public decimal PesoKg { get; set; }
        public byte Tamano { get; set; }
        public string TamanoNombre { get; set; } = string.Empty;
        public string OrigenDir { get; set; } = string.Empty;
        public string DestinoDir { get; set; } = string.Empty;
        public DateTime CreadoEn { get; set; }
        public string NombreRemitente { get; set; } = string.Empty;

        // Datos de la asignación del envío al viaje.
        public DateTime FechaViaje { get; set; }
        public decimal DesvioKm { get; set; }
        public int TiempoExtraMin { get; set; }
        public decimal Ganancia { get; set; }
    }
}
