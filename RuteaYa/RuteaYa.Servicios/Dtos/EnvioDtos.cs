using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Servicios.Dtos;

// DTO exclusivo para recibir los datos que el usuario completa al publicar un envío.
// No incluye UsuarioId ni Estado: esos valores los determina la API.
public sealed class CrearEnvioDto : IValidatableObject
{
    [Required(ErrorMessage = "El origen es obligatorio.")]
    [StringLength(150, ErrorMessage = "El origen no puede superar los 150 caracteres.")]
    public string OrigenDir { get; set; } = string.Empty;

    [Required(ErrorMessage = "El destino es obligatorio.")]
    [StringLength(150, ErrorMessage = "El destino no puede superar los 150 caracteres.")]
    public string DestinoDir { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha límite es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime FechaLimite { get; set; }

    [Range(0.01, 1000, ErrorMessage = "El peso debe ser mayor que cero.")]
    public decimal PesoKg { get; set; }

    [Range(1, 3, ErrorMessage = "Seleccioná un tamaño válido.")]
    public byte Tamano { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(70, ErrorMessage = "La descripción no puede superar los 70 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(0.01, 100000000, ErrorMessage = "El monto ofrecido debe ser mayor que cero.")]
    public decimal Monto { get; set; }

    // Contiene las reglas que dependen de más de una propiedad del formulario.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(OrigenDir) &&
            !string.IsNullOrWhiteSpace(DestinoDir) &&
            string.Equals(OrigenDir.Trim(), DestinoDir.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "El origen y el destino deben ser diferentes.",
                new[] { nameof(OrigenDir), nameof(DestinoDir) });
        }

        if (FechaLimite.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "La fecha límite no puede ser anterior a la fecha actual.",
                new[] { nameof(FechaLimite) });
        }
    }
}

// DTO de salida para no exponer directamente la entidad de Entity Framework.
public sealed class EnvioDto
{
    public int Id { get; set; }
    public string OrigenDir { get; set; } = string.Empty;
    public string DestinoDir { get; set; } = string.Empty;
    public DateTime FechaLimite { get; set; }
    public decimal PesoKg { get; set; }
    public byte Tamano { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public byte Estado { get; set; }
    public DateTime CreadoEn { get; set; }
    public ViajeAsociadoDto? Viaje { get; set; }
    public bool PuedeCancelar { get; set; }
    public bool PuedeEliminar { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
}

// DTO con la información del viaje en el que se asignó el envío.
public sealed class ViajeAsociadoDto
{
    public int Id { get; set; }
    public string OrigenDir { get; set; } = string.Empty;
    public string DestinoDir { get; set; } = string.Empty;
    public TimeOnly HoraSalida { get; set; }
    public TimeOnly HoraLlegada { get; set; }
    public List<string> DiasSemana { get; set; } = new();
    public string? NombreConductor { get; set; }
}

// DTO para recibir los datos cuando un usuario acepta y vincula un envío a su viaje
public sealed class AsignarEnvioDto
{
    [Required(ErrorMessage = "El ID del viaje es obligatorio.")]
    public int ViajeId { get; set; }

    [Required(ErrorMessage = "La fecha del viaje es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime FechaViaje { get; set; }
}