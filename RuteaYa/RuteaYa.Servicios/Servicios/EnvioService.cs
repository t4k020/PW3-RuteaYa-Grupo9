using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios
{
    public interface IEnvioService
    {
        Task<Envio> CrearEnvioAsync(Envio envio);
        Task<Envio> CrearEnvioAsync(Envio envio, int usuarioId);
        Task<Envio?> ObtenerPorIdAsync(int id);
        Task<Envio?> ObtenerPorIdAsync(int id, int usuarioId);
        Task<EnvioDto?> ObtenerDetalleDtoAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken);
        Task<List<Envio>> ObtenerPorUsuarioAsync(int usuarioId);
        Task<List<EnvioDto>> ObtenerListadoPorUsuarioAsync(
            int usuarioId,
            EstadoEnvio? estado,
            CancellationToken cancellationToken);
        Task<bool> EliminarEnvioAsync(int id);
        Task<bool> EliminarEnvioAsync(int id, int usuarioId);
        Task<bool> CancelarEnvioAsync(int id, int usuarioId);

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

        public async Task<EnvioDto?> ObtenerDetalleDtoAsync(
            int id,
            int usuarioId,
            CancellationToken cancellationToken)
        {
            var envio = await _context.Envios
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.Id == id && e.UsuarioId == usuarioId,
                    cancellationToken);

            if (envio == null)
            {
                return null;
            }

            var asignacion = await _context.Asignaciones
                .AsNoTracking()
                .Include(a => a.Viaje!)
                    .ThenInclude(v => v.Usuario)
                .Include(a => a.Viaje!)
                    .ThenInclude(v => v.ViajeDias)
                .FirstOrDefaultAsync(a => a.EnvioId == envio.Id, cancellationToken);

            return CrearDto(envio, asignacion);
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

        public async Task<List<EnvioDto>> ObtenerListadoPorUsuarioAsync(
            int usuarioId,
            EstadoEnvio? estado,
            CancellationToken cancellationToken)
        {
            var consulta = _context.Envios
                .AsNoTracking()
                .Where(e => e.UsuarioId == usuarioId);

            if (estado.HasValue)
            {
                consulta = consulta.Where(e => e.Estado == (byte)estado.Value);
            }

            var envios = await consulta
                .OrderByDescending(e => e.CreadoEn)
                .ToListAsync(cancellationToken);

            return envios
                .Select(envio => CrearDto(envio, asignacion: null))
                .ToList();
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

            // Se eliminan las dependencias explícitamente para no violar
            // la clave foránea de Asignaciones al borrar el envío.
            var asignaciones = await _context.Asignaciones
                .Where(a => a.EnvioId == id)
                .ToListAsync();

            if (asignaciones.Count > 0)
            {
                _context.Asignaciones.RemoveRange(asignaciones);
            }

            var historiales = await _context.EnvioEstadoHistoriales
                .Where(h => h.EnvioId == id)
                .ToListAsync();

            if (historiales.Count > 0)
            {
                _context.EnvioEstadoHistoriales.RemoveRange(historiales);
            }

            _context.Envios.Remove(envio);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelarEnvioAsync(int id, int usuarioId)
        {
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            if (envio == null)
            {
                return false;
            }

            var estadoActual = (EstadoEnvio)envio.Estado;
            if (estadoActual is not (EstadoEnvio.BuscandoConductor or EstadoEnvio.ConductorAsignado))
            {
                throw new InvalidOperationException(
                    "El envío no puede cancelarse en su estado actual.");
            }

            var estadoAnterior = envio.Estado;
            envio.Estado = (byte)EstadoEnvio.Cancelado;
            _context.EnvioEstadoHistoriales.Add(new EnvioEstadoHistorial
            {
                EnvioId = envio.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = envio.Estado,
                Fecha = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        private static string ObtenerNombreEstado(EstadoEnvio estado) => estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            EstadoEnvio.EnCamino => "En camino",
            _ => "Estado desconocido"
        };

        private static string ObtenerNombreDia(byte dia) => dia switch
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

        private static bool DebeMostrarConductor(EstadoEnvio estado) =>
            estado is EstadoEnvio.ConductorAsignado
                or EstadoEnvio.Retirado
                or EstadoEnvio.EnCamino
                or EstadoEnvio.Entregado;

        private static EnvioDto CrearDto(Envio envio, Asignacion? asignacion)
        {
            var estadoEnvio = (EstadoEnvio)envio.Estado;

            return new EnvioDto
            {
                Id = envio.Id,
                OrigenDir = envio.OrigenDir,
                DestinoDir = envio.DestinoDir,
                FechaLimite = envio.FechaLimite,
                PesoKg = envio.PesoKg,
                Tamano = envio.Tamano,
                Descripcion = envio.Descripcion ?? string.Empty,
                Monto = envio.Monto,
                Estado = envio.Estado,
                CreadoEn = envio.CreadoEn,
                Viaje = CrearViajeDto(asignacion, estadoEnvio),
                PuedeCancelar = estadoEnvio is EstadoEnvio.BuscandoConductor or EstadoEnvio.ConductorAsignado,
                PuedeEliminar = estadoEnvio is EstadoEnvio.Cancelado,
                EstadoNombre = ObtenerNombreEstado(estadoEnvio)
            };
        }

        private static ViajeAsociadoDto? CrearViajeDto(Asignacion? asignacion, EstadoEnvio estado)
        {
            if (asignacion?.Viaje is null || estado is EstadoEnvio.BuscandoConductor)
            {
                return null;
            }

            var viaje = asignacion.Viaje;
            string? nombreConductor = null;

            if (DebeMostrarConductor(estado) && viaje.Usuario is not null)
            {
                nombreConductor = $"{viaje.Usuario.Nombre} {viaje.Usuario.Apellido}".Trim();
            }

            return new ViajeAsociadoDto
            {
                Id = viaje.Id,
                OrigenDir = viaje.OrigenDir,
                DestinoDir = viaje.DestinoDir,
                HoraSalida = viaje.HoraSalida,
                HoraLlegada = viaje.HoraLlegada,
                DiasSemana = viaje.ViajeDias
                    .OrderBy(d => d.DiaSemana)
                    .Select(d => ObtenerNombreDia(d.DiaSemana))
                    .ToList(),
                NombreConductor = nombreConductor
            };
        }
    }
}
