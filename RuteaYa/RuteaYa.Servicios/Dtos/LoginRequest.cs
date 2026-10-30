using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Servicios.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string PasswordHash { get; set; } = string.Empty;
    }
}