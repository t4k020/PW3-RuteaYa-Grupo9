using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;

namespace RuteaYa.Repositorios
{
    public class UsuarioRepository
    {
        private RuteaYaDbContext _context;

        public UsuarioRepository()
        {
        }

        public UsuarioRepository(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerUsuarioPorEmail(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<Usuario>> ListarUsuariosAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
