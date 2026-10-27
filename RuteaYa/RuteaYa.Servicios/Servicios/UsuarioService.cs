using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Controllers;

namespace RuteaYa.Servicios.Servicios
{
    public class UsuarioService
    {
        private UsuarioRepository _usuarioRepository;

        public UsuarioService( UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }
        public async Task<bool> AutenticarUsuario(string nombre, string passwordHash)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CrearUsuario(Usuario usuario)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Usuario>> ListarUsuariosAsync()
        {
            return await _usuarioRepository.ListarUsuariosAsync();
        }
    }
}