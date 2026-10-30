using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Servicios
{
    public class UsuarioService
    {
        private readonly UsuarioRepository _usuarioRepository;

        public UsuarioService(UsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<bool> CrearUsuario(RegisterRequest request)
        {
            bool existe = await _usuarioRepository
                .ExisteEmailRegistrado(request.Email);

            if (existe)
                return false;

            var usuario = new Usuario
            {
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Email = request.Email,

                // La contraseña se hashea en el servidor
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),

                CreadoEn = DateTime.UtcNow
            };

            await _usuarioRepository.CrearUsuario(usuario);

            return true;
        }

        public async Task<List<UsuarioDto>> ListarUsuariosAsync()
        {
            var usuarios = await _usuarioRepository.ListarUsuariosAsync();

            return usuarios.Select(usuario => new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                CreadoEn = usuario.CreadoEn
            }).ToList();
        }
    }
}