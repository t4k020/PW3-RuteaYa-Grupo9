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

            try
            {
                await _usuarioRepository.CrearUsuario(usuario);
            }
            catch (DbUpdateException)
            {
                // Dos registros simultáneos con el mismo email pasan la consulta
                // anterior; el índice único de la base rechaza el segundo INSERT.
                // Si el email ya existe es un duplicado (409), si no se propaga.
                if (await _usuarioRepository.ExisteEmailRegistrado(request.Email))
                    return false;

                throw;
            }

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

        public async Task<UsuarioDto?> ObtenerPropioAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObtenerUsuarioPorIdAsync(usuarioId);
            return usuario is null ? null : Mapear(usuario);
        }

        public async Task<bool> ActualizarPropioAsync(int usuarioId, ActualizarUsuarioRequest request)
        {
            var usuario = await _usuarioRepository.ObtenerUsuarioPorIdAsync(usuarioId);
            if (usuario is null) return false;

            var email = request.Email.Trim();
            var existente = await _usuarioRepository.ObtenerUsuarioPorEmail(email);
            if (existente is not null && existente.Id != usuarioId)
                throw new InvalidOperationException("El email ya está registrado.");

            usuario.Nombre = request.Nombre.Trim();
            usuario.Apellido = request.Apellido.Trim();
            usuario.Email = email;
            await _usuarioRepository.GuardarCambiosAsync();
            return true;
        }

        public async Task<bool> EliminarPropioAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObtenerUsuarioPorIdAsync(usuarioId);
            if (usuario is null) return false;
            if (await _usuarioRepository.TieneOperacionesAsync(usuarioId))
                throw new InvalidOperationException("No se puede eliminar una cuenta que tiene viajes o envíos registrados.");
            await _usuarioRepository.EliminarUsuarioAsync(usuario);
            return true;
        }

        private static UsuarioDto Mapear(Usuario usuario) => new()
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            CreadoEn = usuario.CreadoEn
        };
    }
}
