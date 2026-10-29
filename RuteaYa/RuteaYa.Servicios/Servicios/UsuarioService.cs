using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> CrearUsuario(Usuario usuario)
        {
            // Verificar si el correo ya está registrado
            bool existe =await _usuarioRepository.ExisteEmailRegistrado(usuario.Email);

            if (!existe)
            {
                await _usuarioRepository.CrearUsuario(usuario);
            }

            return !existe;
        }

        public async Task<List<Usuario>> ListarUsuariosAsync()
        {
            return await _usuarioRepository.ListarUsuariosAsync();
        }
    }
}