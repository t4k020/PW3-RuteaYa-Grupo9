namespace RuteaYa.Servicios.Dtos
{
    public class LoginResponse
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public string Mensaje { get; set; } = string.Empty;
    }
}