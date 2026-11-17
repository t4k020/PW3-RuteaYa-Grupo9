using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Servicios.Dtos;

// Envío candidato a formar parte de una combinación, con el desvío que implicaría llevarlo.
public sealed class PaqueteOptimizableDto
{
    public int Id { get; set; }
    public string OrigenDir { get; set; } = string.Empty;
    public string DestinoDir { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public decimal Monto { get; set; }
    public decimal DesvioKm { get; set; }
    public int TiempoExtraMin { get; set; }
    public DateTime FechaLimite { get; set; }
}

// Conjunto de envíos evaluado por el optimizador, con sus totales.
public sealed class CombinacionDto
{
    public List<PaqueteOptimizableDto> Paquetes { get; set; } = new();
    public decimal GananciaTotal { get; set; }
    public decimal PesoTotalKg { get; set; }
    public decimal DesvioTotalKm { get; set; }

    // False cuando la suma supera el peso o el desvío disponibles del viaje.
    public bool EsValida { get; set; }
    public string? MotivoRechazo { get; set; }
}

// Resultado completo del optimizador para un viaje en una fecha.
public sealed class OptimizacionDto
{
    public int ViajeId { get; set; }
    public string OrigenDir { get; set; } = string.Empty;
    public string DestinoDir { get; set; } = string.Empty;
    public DateTime FechaViaje { get; set; }

    // Capacidad que le queda al viaje ese día (ya descontado lo asignado).
    public int CupoDisponible { get; set; }
    public decimal PesoDisponibleKg { get; set; }
    public decimal DesvioDisponibleKm { get; set; }

    // Cantidad de oportunidades que entraron al cálculo.
    public int CandidatosEvaluados { get; set; }

    public CombinacionDto? Recomendada { get; set; }
    public List<CombinacionDto> Alternativas { get; set; } = new();
    public List<CombinacionDto> Descartadas { get; set; } = new();
}

// Datos para reservar de una vez todos los envíos de una combinación.
public sealed class ReservarCombinacionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El viaje es obligatorio.")]
    public int ViajeId { get; set; }

    [Required(ErrorMessage = "La fecha del viaje es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime FechaViaje { get; set; }

    [MinLength(1, ErrorMessage = "Elegí al menos un envío.")]
    public List<int> EnviosIds { get; set; } = new();
}
