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
    }
}
