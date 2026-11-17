using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;

namespace RuteaYa.Repositorios
{
    public class UsuarioRepository
    {
        private RuteaYaDbContext _context;

        public UsuarioRepository(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerUsuarioPorEmail(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ExisteEmailRegistrado(string email)
        {
            return await _context.Usuarios.AnyAsync(u => u.Email == email);
        }

        public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
        {
            return await _context.Usuarios.FindAsync(id);
        }

        public async Task<List<Usuario>> ListarUsuariosAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task CrearUsuario(Usuario usuario)
        {
            usuario.CreadoEn = DateTime.Now;
            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

        }

        public async Task GuardarCambiosAsync() => await _context.SaveChangesAsync();

        public async Task EliminarUsuarioAsync(Usuario usuario)
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TieneOperacionesAsync(int usuarioId) =>
            await _context.Viajes.AnyAsync(v => v.UsuarioId == usuarioId) ||
            await _context.Envios.AnyAsync(e => e.UsuarioId == usuarioId);
    }
}
