using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Servicios.Dtos;

// DTO exclusivo para recibir los datos que el conductor completa al registrar un viaje.
// No incluye UsuarioId, Activo ni coordenadas: esos valores los determina la API.
public sealed class CrearViajeDto : IValidatableObject
{
    [Required(ErrorMessage = "El origen es obligatorio.")]
    [StringLength(150, ErrorMessage = "El origen no puede superar los 150 caracteres.")]
    public string OrigenDir { get; set; } = string.Empty;

    [Required(ErrorMessage = "El destino es obligatorio.")]
    [StringLength(150, ErrorMessage = "El destino no puede superar los 150 caracteres.")]
    public string DestinoDir { get; set; } = string.Empty;

    [Required(ErrorMessage = "La hora de salida es obligatoria.")]
    public TimeOnly? HoraSalida { get; set; }

    [Required(ErrorMessage = "La hora de llegada es obligatoria.")]
    public TimeOnly? HoraLlegada { get; set; }

    [MinLength(1, ErrorMessage = "Seleccioná al menos un día de la semana.")]
    public List<byte> DiasSemana { get; set; } = new();

    [Range(1, 1000, ErrorMessage = "La cantidad máxima de paquetes debe ser mayor que cero.")]
    public int MaxPaquetes { get; set; }

    [Range(0.01, 100000, ErrorMessage = "El peso máximo debe ser mayor que cero.")]
    public decimal PesoMaxKg { get; set; }

    [Range(0, 100000, ErrorMessage = "El desvío máximo no puede ser negativo.")]
    public decimal DesvioMaxKm { get; set; }

    // Las paradas se reciben en el orden en que el usuario las agregó.
    public List<ParadaViajeDto> Paradas { get; set; } = new();

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

        if (HoraSalida.HasValue && HoraLlegada.HasValue && HoraLlegada.Value <= HoraSalida.Value)
        {
            yield return new ValidationResult(
                "La hora de llegada debe ser posterior a la hora de salida.",
                new[] { nameof(HoraLlegada) });
        }

        var dias = DiasSemana ?? new List<byte>();
        if (dias.Any(dia => dia < 1 || dia > 7))
        {
            yield return new ValidationResult(
                "Los días seleccionados no son válidos.",
                new[] { nameof(DiasSemana) });
        }

        if (dias.Distinct().Count() != dias.Count)
        {
            yield return new ValidationResult(
                "No se puede repetir un día de la semana.",
                new[] { nameof(DiasSemana) });
        }

        var origen = OrigenDir?.Trim();
        var destino = DestinoDir?.Trim();
        var paradas = Paradas ?? new List<ParadaViajeDto>();
        var direcciones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < paradas.Count; i++)
        {
            var parada = paradas[i];
            var direccion = parada?.Direccion?.Trim();

            if (string.IsNullOrWhiteSpace(direccion))
            {
                yield return new ValidationResult(
                    "La dirección de la parada es obligatoria.",
                    new[] { $"Paradas[{i}].Direccion" });
                continue;
            }

            if (!string.IsNullOrWhiteSpace(origen) &&
                string.Equals(direccion, origen, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "La parada no puede ser igual al origen.",
                    new[] { $"Paradas[{i}].Direccion" });
            }

            if (!string.IsNullOrWhiteSpace(destino) &&
                string.Equals(direccion, destino, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "La parada no puede ser igual al destino.",
                    new[] { $"Paradas[{i}].Direccion" });
            }

            if (!direcciones.Add(direccion))
            {
                yield return new ValidationResult(
                    "No se puede repetir una parada.",
                    new[] { $"Paradas[{i}].Direccion" });
            }
        }
    }
}

// Dirección de una parada intermedia. Las coordenadas se completarán
// cuando se integre el proveedor de rutas.
public sealed class ParadaViajeDto
{
    [Required(ErrorMessage = "La dirección de la parada es obligatoria.")]
    [StringLength(150, ErrorMessage = "La dirección no puede superar los 150 caracteres.")]
    public string Direccion { get; set; } = string.Empty;
}
