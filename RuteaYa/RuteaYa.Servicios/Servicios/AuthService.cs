using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Controllers;

namespace RuteaYa.Servicios.Servicios
{
    public class AuthService : IAuthService
    {
        private  UsuarioRepository _usuarioRepository;

        public AuthService()
        {
            _usuarioRepository = new UsuarioRepository();
        }

        public async Task<bool> AutenticarUsuario(string email, string password)
        {
            Usuario usuario = await _usuarioRepository.ObtenerUsuarioPorEmail(email);

            if (usuario == null)
                return false;

            // Verificar la contraseña ingresada
            // contra el hash almacenado en la base de datos.
            return BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        }
    }
}