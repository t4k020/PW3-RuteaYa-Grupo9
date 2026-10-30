using System.ComponentModel.DataAnnotations;

namespace RuteaYa.Servicios.Dtos
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;
    }
}