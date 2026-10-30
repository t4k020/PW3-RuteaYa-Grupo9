
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Controllers;

namespace RuteaYa.Servicios.Servicios
{
    public class AuthService : IAuthService
    {
        private readonly UsuarioRepository _usuarioRepository;

        public AuthService(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<Usuario?> AutenticarUsuario(
            string email,
            string password)
        {
            var usuario = await _usuarioRepository
                .ObtenerUsuarioPorEmail(email);

            if (usuario == null)
                return null;

            bool autenticado = BCrypt.Net.BCrypt.Verify(
                password,
                usuario.PasswordHash);

            return autenticado ? usuario : null;
        }
    }
}
