using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace RuteaYa.Servicios
{
    public interface IEnvioService
    {
        Task<Envio> CrearEnvioAsync(Envio envio);
        Task<Envio?> ObtenerPorIdAsync(int id);

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

        public async Task<Envio?> ObtenerPorIdAsync(int id)
        {
            
            return await _context.Envios.FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}
