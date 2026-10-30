using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios
{
    public interface IViajeService
    {
        Task<Viaje> CrearViajeAsync(Viaje viaje);
        Task<Viaje?> ObtenerPorIdAsync(int id);
        Task<bool> ActualizarViajeAsync(int id, Viaje viaje);
        Task<bool> EliminarViajeAsync(int id);
        Task<IEnumerable<Viaje>> ObtenerTodosAsync();
        Task<IEnumerable<ViajeListadoDto>> ObtenerParaListadoAsync(bool? soloActivo);
        Task<bool> DesactivarViajeAsync(int id);
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

        public async Task<IEnumerable<Viaje>> ObtenerTodosAsync()
        {
            return await _context.Viajes
                .Include(v => v.Usuario)
                .Include(v => v.Paradas)
                .Include(v => v.ViajeDias)
                .ToListAsync();
        }

        public async Task<IEnumerable<ViajeListadoDto>> ObtenerParaListadoAsync(bool? soloActivo)
        {
            // ID temporal para simular al usuario logueado mientras tu compañero termina el login
            int usuarioLogueadoId = 1;

            var query = _context.Viajes
                .Where(v => v.UsuarioId == usuarioLogueadoId);

            if (soloActivo.HasValue)
            {
                query = query.Where(v => v.Activo == soloActivo.Value);
            }

            // Traemos solo los viajes de este usuario de la base de datos
            var viajes = await query
                .Include(v => v.ViajeDias)
                .ToListAsync();

            // 2. Mapeamos al DTO procesando los días en memoria (C# puro)
            var viajesDto = viajes.Select(v => new ViajeListadoDto
            {
                Id = v.Id,
                OrigenDir = v.OrigenDir,
                DestinoDir = v.DestinoDir,
                HoraSalida = v.HoraSalida,
                HoraLlegada = v.HoraLlegada,
                Activo = v.Activo,
                DiasSemana = v.ViajeDias.Select(vd => ObtenerNombreDia(vd.DiaSemana)).ToList()
            }).ToList();

            return viajesDto;
        }

        private string ObtenerNombreDia(byte dia)
        {
            return dia switch
            {
                1 => "Lun",
                2 => "Mar",
                3 => "Mié",
                4 => "Jue",
                5 => "Vie",
                6 => "Sáb",
                7 => "Dom",
                _ => "Día"
            };
        }

        //Método para desactivar un viaje, lo saca de la lista de viajes activos, pero no lo elimina de la base de datos
        public async Task<bool> DesactivarViajeAsync(int id)
        {
            var viaje = await _context.Viajes.FindAsync(id);
            if (viaje == null)
            {
                return false;
            }
            viaje.Activo = false;
            _context.Viajes.Update(viaje);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
