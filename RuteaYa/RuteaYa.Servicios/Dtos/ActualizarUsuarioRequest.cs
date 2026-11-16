using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Servicios.Dtos;

public sealed class ActualizarUsuarioRequest
{
    [Required, StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Apellido { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;
}
