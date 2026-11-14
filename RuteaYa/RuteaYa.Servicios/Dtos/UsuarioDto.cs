namespace RuteaYa.Servicios.Dtos
{
    public class UsuarioDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Apellido { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreadoEn { get; set; }
    }
}