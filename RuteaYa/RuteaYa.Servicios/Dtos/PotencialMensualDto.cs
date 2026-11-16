namespace RuteaYa.Servicios.Dtos;

public sealed class PotencialMensualDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Criterio { get; set; } = "ganancia";
    public decimal GananciaPotencial { get; set; }
    public decimal GananciaEfectiva { get; set; }
    public int ViajesAnalizados { get; set; }
    public int PaquetesPosibles { get; set; }
    public decimal PromedioPorViaje { get; set; }
    public List<PotencialSemanaDto> Semanas { get; set; } = new();
    public List<PotencialViajeDto> Viajes { get; set; } = new();
}

public sealed class PotencialSemanaDto
{
    public int Numero { get; set; }
    public decimal Ganancia { get; set; }
}

public sealed class PotencialViajeDto
{
    public int ViajeId { get; set; }
    public DateTime Fecha { get; set; }
    public string OrigenDir { get; set; } = string.Empty;
    public string DestinoDir { get; set; } = string.Empty;
    public int Paquetes { get; set; }
    public decimal Ganancia { get; set; }
    public decimal DesvioKm { get; set; }
}
