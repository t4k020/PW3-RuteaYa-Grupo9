namespace RuteaYa.Servicios.Dtos;

// DTO principal que transporta todo el resumen mostrado en la pantalla de inicio.
public class HomeDashboardDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public int CantidadViajes { get; set; }
    public int CantidadEnvios { get; set; }
    public int CantidadOportunidades { get; set; }
    public decimal PotencialGanancias { get; set; }
    public decimal GananciaEfectivaMes { get; set; }
    public List<HomeViajeDto> Viajes { get; set; } = new();
    public List<HomeEnvioDto> Envios { get; set; } = new();
    public List<HomeOportunidadDto> Oportunidades { get; set; } = new();
}

// Datos mínimos de cada viaje que se muestra en el dashboard.
public class HomeViajeDto
{
    public int Id { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public TimeOnly HoraSalida { get; set; }
    public TimeOnly HoraLlegada { get; set; }
    public bool Activo { get; set; }
    public List<string> DiasSemana { get; set; } = new();
}

// Datos mínimos de los envíos propios que todavía requieren seguimiento.
public class HomeEnvioDto
{
    public int Id { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; }
    public decimal Monto { get; set; }
    public byte Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
}

// Datos de envíos de otros usuarios que pueden convertirse en oportunidades.
public class HomeOportunidadDto
{
    public int Id { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; }
    public decimal Monto { get; set; }
    public decimal PesoKg { get; set; }
}
