using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace RuteaYa.Servicios
{
    public interface IViajeService
    {
        Task<Viaje> CrearViajeAsync(Viaje viaje);
        Task<Viaje?> ObtenerPorIdAsync(int id);
        Task<bool> ActualizarViajeAsync(int id, Viaje viaje);
        Task<bool> EliminarViajeAsync(int id);
    }

    public class ViajeService : IViajeService
    {
        private readonly RuteaYaDbContext _context;

        public ViajeService(RuteaYaDbContext context)
        {
            _context = context;
        }

        public async Task<Viaje> CrearViajeAsync(Viaje viaje)
        {
            _context.Viajes.Add(viaje);
            await _context.SaveChangesAsync();
            return viaje;
        }

        public async Task<Viaje?> ObtenerPorIdAsync(int id)
        {
            return await _context.Viajes
                .Include(v => v.Usuario)
                .Include(v => v.Paradas)
                .Include(v => v.ViajeDias)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<bool> ActualizarViajeAsync(int id, Viaje viajeModificado)
        {
            var viajeExistente = await _context.Viajes.FindAsync(id);
            if (viajeExistente == null)
            {
                return false;
            }

            // Actualizamos todas las propiedades con los datos nuevos
            viajeExistente.UsuarioId = viajeModificado.UsuarioId;
            viajeExistente.OrigenDir = viajeModificado.OrigenDir;
            viajeExistente.OrigenLat = viajeModificado.OrigenLat;
            viajeExistente.OrigenLng = viajeModificado.OrigenLng;
            viajeExistente.DestinoDir = viajeModificado.DestinoDir;
            viajeExistente.DestinoLat = viajeModificado.DestinoLat;
            viajeExistente.DestinoLng = viajeModificado.DestinoLng;
            viajeExistente.HoraSalida = viajeModificado.HoraSalida;
            viajeExistente.HoraLlegada = viajeModificado.HoraLlegada;
            viajeExistente.MaxPaquetes = viajeModificado.MaxPaquetes;
            viajeExistente.PesoMaxKg = viajeModificado.PesoMaxKg;
            viajeExistente.DesvioMaxKm = viajeModificado.DesvioMaxKm;
            viajeExistente.DistanciaKm = viajeModificado.DistanciaKm;
            viajeExistente.DuracionMin = viajeModificado.DuracionMin;
            viajeExistente.Activo = viajeModificado.Activo;

            _context.Viajes.Update(viajeExistente);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EliminarViajeAsync(int id)
        {
            var viaje = await _context.Viajes.FindAsync(id);

            if (viaje == null)
            {
                return false;
            }

            _context.Viajes.Remove(viaje);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
