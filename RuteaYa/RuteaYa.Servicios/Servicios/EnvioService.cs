using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace RuteaYa.Servicios
{
    public interface IEnvioService
    {
        Task<Envio> CrearEnvioAsync(Envio envio);
        Task<Envio> CrearEnvioAsync(Envio envio, int usuarioId);
        Task<Envio?> ObtenerPorIdAsync(int id);
        Task<Envio?> ObtenerPorIdAsync(int id, int usuarioId);
        Task<List<Envio>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<bool> EliminarEnvioAsync(int id);
        Task<bool> EliminarEnvioAsync(int id, int usuarioId);

    }
    public class EnvioService : IEnvioService {
        private readonly RuteaYaDbContext _context;

        public EnvioService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<Envio> CrearEnvioAsync(Envio envio)
        {
            envio.CreadoEn = DateTime.Now;
            _context.Envios.Add(envio);
            await _context.SaveChangesAsync();
            return envio;
        }

        public async Task<Envio> CrearEnvioAsync(Envio envio, int usuarioId)
        {
            // La validación se repite en el servicio porque la API es la autoridad
            // y no debe depender únicamente de las validaciones del formulario MVC.
            var usuarioExiste = await _context.Usuarios
                .AnyAsync(usuario => usuario.Id == usuarioId);

            if (!usuarioExiste)
            {
                throw new InvalidOperationException("El usuario autenticado no existe.");
            }

            if (string.Equals(
                    envio.OrigenDir.Trim(),
                    envio.DestinoDir.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El origen y el destino deben ser diferentes.");
            }

            if (envio.FechaLimite.Date < DateTime.Today)
            {
                throw new InvalidOperationException("La fecha límite no puede ser anterior a la fecha actual.");
            }

            if (envio.PesoKg <= 0 || envio.Monto <= 0)
            {
                throw new InvalidOperationException("El peso y el monto ofrecido deben ser mayores que cero.");
            }

            if (envio.Tamano is < 1 or > 3)
            {
                throw new InvalidOperationException("El tamaño del envío no es válido.");
            }

            if (string.IsNullOrWhiteSpace(envio.Descripcion))
            {
                throw new InvalidOperationException("La descripción es obligatoria.");
            }

            if (envio.Descripcion.Trim().Length > 70)
            {
                throw new InvalidOperationException("La descripción no puede superar los 70 caracteres.");
            }

            envio.UsuarioId = usuarioId;
            envio.Usuario = null;
            // Todos los envíos nuevos comienzan buscando conductor.
            envio.Estado = (byte)EstadoEnvio.BuscandoConductor;
            envio.CreadoEn = DateTime.Now;
            _context.Envios.Add(envio);
            await _context.SaveChangesAsync();
            return envio;
        }

        public async Task<Envio?> ObtenerPorIdAsync(int id)
        {
            
            return await _context.Envios.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Envio?> ObtenerPorIdAsync(int id, int usuarioId)
        {
            // Si el envío pertenece a otro usuario, se comporta como no encontrado.
            return await _context.Envios
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);
        }

        public async Task<List<Envio>> ObtenerPorUsuarioAsync(int usuarioId)
        {
            // AsNoTracking mejora las consultas de solo lectura y evita exponer cambios accidentales.
            return await _context.Envios
                .AsNoTracking()
                .Where(e => e.UsuarioId == usuarioId)
                .OrderByDescending(e => e.CreadoEn)
                .ToListAsync();
        }

        public async Task<bool> EliminarEnvioAsync(int id)
        {
            var envio = await _context.Envios.FindAsync(id);

            if (envio == null)
            {
                return false; 
            }

            _context.Envios.Remove(envio);
 
            await _context.SaveChangesAsync();

            return true; 
        }

        public async Task<bool> EliminarEnvioAsync(int id, int usuarioId)
        {
            // La condición de usuario impide eliminar envíos de terceros.
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if (envio == null)
            {
                return false;
            }

            _context.Envios.Remove(envio);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
