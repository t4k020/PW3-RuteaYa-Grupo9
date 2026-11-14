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
        Task<bool> ActualizarViajeAsync(int id, Viaje viaje, int usuarioId);
        Task<bool> EliminarViajeAsync(int id, int usuarioId);
        Task<IEnumerable<Viaje>> ObtenerTodosAsync();
        Task<IEnumerable<ViajeListadoDto>> ObtenerParaListadoAsync(bool? soloActivo, int usuarioId);
        Task<bool> DesactivarViajeAsync(int id, int usuarioId);
        Task<bool> ActivarViajeAsync(int id, int usuarioId);
        Task<ViajeDetalleDto?> ObtenerDetallePorIdAsync(int id, int usuarioId);
        Task<List<Envio>> ObtenerTransportesAsignadosAsync(int usuarioId);
        Task<bool> CambiarEstadoTransporteAsync(int envioId, string accion, int usuarioId);
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

        public async Task<bool> ActualizarViajeAsync(int id, Viaje viajeModificado, int usuarioId)
        {
            var viajeExistente = await _context.Viajes.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
            
            if (viajeExistente == null)
                return false;
            

            // Actualizamos todas las propiedades con los datos nuevos
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

        public async Task<bool> EliminarViajeAsync(int id, int usuarioId)
        {
            var viaje = await _context.Viajes.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);

            if (viaje == null)
            {
                return false;
            }

            // Borrado lógico: el viaje desaparece de los listados pero se conserva
            // para no romper el historial de envíos y asignaciones.
            viaje.EliminadoEn = DateTime.Now;
            _context.Viajes.Update(viaje);
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

        public async Task<IEnumerable<ViajeListadoDto>> ObtenerParaListadoAsync(bool? soloActivo, int usuarioId)
        {
            var query = _context.Viajes
                .Where(v => v.UsuarioId == usuarioId);

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
        public async Task<bool> DesactivarViajeAsync(int id, int usuarioId)
        {
            var viaje = await _context.Viajes.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
            if (viaje == null)
            {
                return false;
            }
            viaje.Activo = false;
            _context.Viajes.Update(viaje);
            await _context.SaveChangesAsync();
            return true;
        }

        //Vuelve a poner en curso un viaje que había sido desactivado
        public async Task<bool> ActivarViajeAsync(int id, int usuarioId)
        {
            var viaje = await _context.Viajes.FirstOrDefaultAsync(v => v.Id == id && v.UsuarioId == usuarioId);
            if (viaje == null)
            {
                return false;
            }
            viaje.Activo = true;
            _context.Viajes.Update(viaje);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ViajeDetalleDto?> ObtenerDetallePorIdAsync(int id, int usuarioId)
        {
            
            var viaje = await _context.Viajes
                .Where(v => v.Id == id && v.UsuarioId == usuarioId)
                .Include(v => v.ViajeDias)
                .Include(v => v.Paradas)
                .Include(v => v.Asignaciones)
                    .ThenInclude(a => a.Envio) 
                .FirstOrDefaultAsync();

            if (viaje == null) return null;

            // Mapeamos a nuestro DTO de detalle
            var detalleDto = new ViajeDetalleDto
            {
                Id = viaje.Id,
                OrigenDir = viaje.OrigenDir,
                OrigenLat = viaje.OrigenLat,
                OrigenLng = viaje.OrigenLng,
                DestinoDir = viaje.DestinoDir,
                DestinoLat = viaje.DestinoLat,
                DestinoLng = viaje.DestinoLng,
                HoraSalida = viaje.HoraSalida,
                HoraLlegada = viaje.HoraLlegada,
                Activo = viaje.Activo,
                MaxPaquetes = viaje.MaxPaquetes,
                PesoMaximoKg = viaje.PesoMaxKg,
                DesvioMaxKm = viaje.DesvioMaxKm,
                DistanciaKm = viaje.DistanciaKm,
                DuracionMin = viaje.DuracionMin,
                DiasSemana = viaje.ViajeDias
                    .OrderBy(vd => vd.DiaSemana)
                    .Select(vd => ObtenerNombreDia(vd.DiaSemana))
                    .ToList(),

                // Las paradas se muestran en el orden del recorrido.
                Paradas = viaje.Paradas
                    .OrderBy(p => p.Orden)
                    .Select(p => new ParadaDto
                    {
                        Id = p.Id,
                        Orden = p.Orden,
                        Direccion = p.Direccion,
                        Lat = p.Lat,
                        Lng = p.Lng
                    })
                    .ToList(),

                // Mapeamos los envíos (paquetes) asignados a este viaje
                PaquetesAsignados = viaje.Asignaciones.Select(a => new PaqueteDto
                {
                    Id = a.Envio.Id,
                    Descripcion = a.Envio.Descripcion ?? string.Empty,
                    Estado = a.Envio.Estado,
                    EstadoNombre = ((EstadoEnvio)a.Envio.Estado).ObtenerNombre(),
                    FechaLimite = a.Envio.FechaLimite,
                    Monto = a.Envio.Monto,
                    OrigenDir = a.Envio.OrigenDir,
                    DestinoDir = a.Envio.DestinoDir
                }).ToList()
            };

            return detalleDto;
        }

        public async Task<List<Envio>> ObtenerTransportesAsignadosAsync(int usuarioId)
        {
            // Buscamos envíos donde el viaje asociado pertenezca al usuario logueado
            return await _context.Envios
                .AsNoTracking()
                .Where(e => _context.Asignaciones.Any(a => a.EnvioId == e.Id && a.Viaje.UsuarioId == usuarioId))
                // Excluimos los que están cancelados o buscando conductor
                .Where(e => e.Estado != (byte)EstadoEnvio.BuscandoConductor && e.Estado != (byte)EstadoEnvio.Cancelado)
                .OrderByDescending(e => e.CreadoEn)
                .ToListAsync();
        }

        public async Task<bool> CambiarEstadoTransporteAsync(int envioId, string accion, int usuarioId)
        {
            // Validar que el envío pertenezca a un viaje de este usuario
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == envioId &&
                    _context.Asignaciones.Any(a => a.EnvioId == e.Id && a.Viaje.UsuarioId == usuarioId));

            if (envio == null) throw new InvalidOperationException("Envío no encontrado o no te pertenece.");

            var estadoActual = (EstadoEnvio)envio.Estado;
            byte nuevoEstado = 0;

            switch (accion.ToLower())
            {
                case "retirado":
                    if (estadoActual != EstadoEnvio.ConductorAsignado) throw new InvalidOperationException("El paquete no está listo para retirar.");
                    nuevoEstado = (byte)EstadoEnvio.Retirado;
                    break;
                case "en-camino":
                    if (estadoActual != EstadoEnvio.Retirado) throw new InvalidOperationException("Primero debes marcar el paquete como retirado.");
                    nuevoEstado = (byte)EstadoEnvio.EnCamino;
                    break;
                case "entregado":
                    if (estadoActual != EstadoEnvio.EnCamino) throw new InvalidOperationException("El paquete debe estar en camino para entregarse.");
                    nuevoEstado = (byte)EstadoEnvio.Entregado;
                    break;
                default:
                    throw new InvalidOperationException("Acción no válida.");
            }

            var estadoAnterior = envio.Estado;
            envio.Estado = nuevoEstado;

            _context.EnvioEstadoHistoriales.Add(new EnvioEstadoHistorial
            {
                EnvioId = envio.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = nuevoEstado,
                Fecha = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
