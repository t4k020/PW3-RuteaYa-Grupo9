using RuteaYa.Entidades;
using RuteaYa.Repositorios;

namespace RuteaYa.Servicios
{
    public interface IEnvioService
    {
        Task<Envio> CrearEnvioAsync(Envio envio);

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
    }
}
