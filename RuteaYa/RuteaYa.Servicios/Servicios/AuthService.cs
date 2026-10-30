using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Controllers;

namespace RuteaYa.Servicios.Servicios
{
    public class AuthService : IAuthService
    {
        private UsuarioRepository _usuarioRepository;

        public AuthService(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<Usuario> AutenticarUsuario(string email, string password)
        {
            Usuario usuario = await _usuarioRepository.ObtenerUsuarioPorEmail(email);
            bool autenticado = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

            if (autenticado)
                return usuario;

            return null;
        }
    }
}