using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using Microsoft.EntityFrameworkCore;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios.Rutas;

namespace RuteaYa.Servicios
{
    public interface IEnvioService
    {
        Task<Envio> CrearEnvioAsync(
            CrearEnvioDto solicitud,
            int usuarioId,
            CancellationToken cancellationToken = default);
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
        Task<bool> ActualizarEnvioAsync(int id, Envio envioActualizado, int usuarioId);
        Task<bool> CancelarEnvioAsync(int id, int usuarioId);

        Task<bool> VincularEnvioAViajeAsync(int envioId, int viajeId, DateTime fechaViaje, int usuarioId);
        Task<List<Envio>> ObtenerOportunidadesDisponiblesAsync(int usuarioId);
        Task<OportunidadesResultadoDto?> ObtenerOportunidadesParaViajeAsync(int viajeId, DateTime fechaViaje, int usuarioId, string orden, CancellationToken cancellationToken);
        Task<EnvioDto?> ObtenerDetalleOportunidadAsync(int id, int usuarioActualId);
        Task<DetalleOportunidadViajeDto?> ObtenerDetalleOportunidadParaViajeAsync(int envioId, int viajeId, DateTime fechaViaje, int usuarioId, CancellationToken cancellationToken);
    }
    public class EnvioService : IEnvioService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IRutaService _rutaService;

        public EnvioService(RuteaYaDbContext context, IRutaService rutaService)
        {
            _context = context;
            _rutaService = rutaService;
        }

        public async Task<Envio> CrearEnvioAsync(
            CrearEnvioDto solicitud,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            // La validación se repite en el servicio porque la API es la autoridad
            // y no debe depender únicamente de las validaciones del formulario MVC.
            var usuarioExiste = await _context.Usuarios
                .AnyAsync(usuario => usuario.Id == usuarioId, cancellationToken);

            if (!usuarioExiste)
            {
                throw new InvalidOperationException("El usuario autenticado no existe.");
            }

            var origenDir = solicitud.OrigenDir.Trim();
            var destinoDir = solicitud.DestinoDir.Trim();

            if (string.Equals(origenDir, destinoDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El origen y el destino deben ser diferentes.");
            }

            if (solicitud.FechaLimite.Date < DateTime.Today)
            {
                throw new InvalidOperationException("La fecha límite no puede ser anterior a la fecha actual.");
            }

            if (solicitud.PesoKg <= 0 || solicitud.Monto <= 0)
            {
                throw new InvalidOperationException("El peso y el monto ofrecido deben ser mayores que cero.");
            }

            if (solicitud.Tamano is < 1 or > 3)
            {
                throw new InvalidOperationException("El tamaño del envío no es válido.");
            }

            if (string.IsNullOrWhiteSpace(solicitud.Descripcion))
            {
                throw new InvalidOperationException("La descripción es obligatoria.");
            }

            if (solicitud.Descripcion.Trim().Length > 70)
            {
                throw new InvalidOperationException("La descripción no puede superar los 70 caracteres.");
            }

            // Las coordenadas las resuelve el servidor: el cliente sólo manda direcciones.
            var origen = await GeocodificarAsync(origenDir, "origen", cancellationToken);
            var destino = await GeocodificarAsync(destinoDir, "destino", cancellationToken);

            var envio = new Envio
            {
                UsuarioId = usuarioId,
                OrigenDir = origenDir,
                OrigenLat = origen.Lat,
                OrigenLng = origen.Lng,
                DestinoDir = destinoDir,
                DestinoLat = destino.Lat,
                DestinoLng = destino.Lng,
                FechaLimite = solicitud.FechaLimite.Date,
                PesoKg = solicitud.PesoKg,
                Tamano = solicitud.Tamano,
                Descripcion = solicitud.Descripcion.Trim(),
                Monto = solicitud.Monto,
                // Todos los envíos nuevos comienzan buscando conductor.
                Estado = (byte)EstadoEnvio.BuscandoConductor,
                CreadoEn = DateTime.Now
            };

            _context.Envios.Add(envio);
            await _context.SaveChangesAsync(cancellationToken);

            return envio;
        }

        private async Task<CoordenadaDto> GeocodificarAsync(
            string direccion,
            string descripcion,
            CancellationToken cancellationToken)
        {
            var coordenada = await _rutaService.GeocodificarAsync(direccion, cancellationToken);

            if (coordenada is null)
            {
                throw new InvalidOperationException(
                    $"No se pudo ubicar la dirección de {descripcion}: {direccion}.");
            }

            return coordenada;
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

        public async Task<EnvioDto?> ObtenerDetalleOportunidadAsync(int id, int usuarioActualId)
        {
            var envio = await _context.Envios
                .AsNoTracking()
                .Include(e => e.Usuario)
                .Where(e => e.Id == id
                         && e.UsuarioId != usuarioActualId
                         && e.Estado == (byte)EstadoEnvio.BuscandoConductor)
                .FirstOrDefaultAsync();

            if (envio == null) return null;


            return CrearDto(envio, asignacion: null);
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

        public async Task<bool> ActualizarEnvioAsync(int id, Envio datos, int usuarioId)
        {
            var envio = await _context.Envios
                .FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == usuarioId);

            // Si no existe o es de otro usuario, se comporta como no encontrado.
            if (envio == null)
            {
                return false;
            }

            // Solo se edita mientras nadie lo tomó.
            if ((EstadoEnvio)envio.Estado != EstadoEnvio.BuscandoConductor
                || await _context.Asignaciones.AnyAsync(a => a.EnvioId == id))
            {
                throw new InvalidOperationException(
                    "El envío solo puede editarse mientras busca conductor.");
            }

            if (string.Equals(datos.OrigenDir.Trim(), datos.DestinoDir.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El origen y el destino deben ser diferentes.");

            if (datos.FechaLimite.Date < DateTime.Today)
                throw new InvalidOperationException("La fecha límite no puede ser anterior a la fecha actual.");

            if (datos.PesoKg <= 0 || datos.Monto <= 0)
                throw new InvalidOperationException("El peso y el monto ofrecido deben ser mayores que cero.");

            if (datos.Tamano is < 1 or > 3)
                throw new InvalidOperationException("El tamaño del envío no es válido.");

            if (string.IsNullOrWhiteSpace(datos.Descripcion))
                throw new InvalidOperationException("La descripción es obligatoria.");

            if (datos.Descripcion.Trim().Length > 70)
                throw new InvalidOperationException("La descripción no puede superar los 70 caracteres.");

            envio.OrigenDir = datos.OrigenDir.Trim();
            envio.DestinoDir = datos.DestinoDir.Trim();
            envio.FechaLimite = datos.FechaLimite.Date;
            envio.PesoKg = datos.PesoKg;
            envio.Tamano = datos.Tamano;
            envio.Descripcion = datos.Descripcion.Trim();
            envio.Monto = datos.Monto;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // RowVersion: otro conductor lo tomó mientras se editaba.
                throw new InvalidOperationException(
                    "El envío cambió mientras lo editabas. Volvé a intentarlo.");
            }

            return true;
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

        public async Task<bool> VincularEnvioAViajeAsync(int envioId, int viajeId, DateTime fechaViaje, int usuarioId)
        {
            // no debe estar cancelado ni ya asignado
            var envio = await _context.Envios.FirstOrDefaultAsync(e => e.Id == envioId);
            if (envio == null)
            {
                throw new InvalidOperationException("El envío no existe.");
            }

            if (envio.Estado == (byte)EstadoEnvio.Cancelado || envio.Estado == (byte)EstadoEnvio.ConductorAsignado)
            {
                throw new InvalidOperationException("El envío no está disponible (ya fue asignado o cancelado).");
            }

            // Verificar que no tenga ya un registro en Asignaciones
            bool yaAsignado = await _context.Asignaciones.AnyAsync(a => a.EnvioId == envioId);
            if (yaAsignado)
            {
                throw new InvalidOperationException("El envío ya cuenta con una asignación activa.");
            }

            // que el viaje exista y pertenezca al usuario 
            var viaje = await _context.Viajes
                .Include(v => v.ViajeDias)
                .FirstOrDefaultAsync(v => v.Id == viajeId);

            if (viaje == null)
            {
                throw new InvalidOperationException("El viaje seleccionado no existe.");
            }

            if (viaje.UsuarioId != usuarioId)
            {
                throw new InvalidOperationException("No puedes asignar envíos a un viaje que no te pertenece.");
            }

            if (!viaje.Activo)
            {
                throw new InvalidOperationException("El viaje no se encuentra activo.");
            }

            //  Validar la fecha 
            byte diaSemanaFecha = fechaViaje.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fechaViaje.DayOfWeek;
            bool diaValido = viaje.ViajeDias.Any(d => d.DiaSemana == diaSemanaFecha);

            if (!diaValido)
            {
                throw new InvalidOperationException("La fecha seleccionada no coincide con los días programados para este viaje.");
            }

            if (fechaViaje.Date < DateTime.Today)
            {
                throw new InvalidOperationException("No se puede asignar un envío a una fecha pasada.");
            }

            // que no supere la cantidad máxima de envíos 
            int cantidadAsignadosActuales = await _context.Asignaciones
                .Where(a => a.ViajeId == viajeId && a.FechaViaje.Date == fechaViaje.Date)
                .CountAsync();

            if (cantidadAsignadosActuales >= viaje.MaxPaquetes)
            {
                throw new InvalidOperationException("El viaje ha alcanzado el límite máximo de paquetes para esa fecha.");
            }

            // que no supere el peso máximo 
            decimal pesoActualAsignados = await _context.Asignaciones
                .Where(a => a.ViajeId == viajeId && a.FechaViaje.Date == fechaViaje.Date)
                .Join(_context.Envios, a => a.EnvioId, e => e.Id, (a, e) => e.PesoKg)
                .SumAsync();

            if (pesoActualAsignados + envio.PesoKg > viaje.PesoMaxKg)
            {
                throw new InvalidOperationException($"El peso total excedería el límite máximo del viaje ({viaje.PesoMaxKg} kg).");
            }

            // Crea la asignación
            var asignacion = new Asignacion
            {
                EnvioId = envio.Id,
                ViajeId = viaje.Id,
                FechaViaje = fechaViaje.Date,
                DesvioKm = 0.0m, // cambiar cuando haya geolocalizacion
                TiempoExtraMin = 0,
                Ganancia = envio.Monto,
                AsignadoEn = DateTime.Now
            };

            _context.Asignaciones.Add(asignacion);

            // Registrar cambio de estado del envío a ConductorAsignado
            var estadoAnterior = envio.Estado;
            envio.Estado = (byte)EstadoEnvio.ConductorAsignado;

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

        public async Task<List<Envio>> ObtenerOportunidadesDisponiblesAsync(int usuarioId)
        {
            return await _context.Envios
                .AsNoTracking()
                .Where(e => e.UsuarioId != usuarioId
                         && e.Estado == (byte)EstadoEnvio.BuscandoConductor
                         && !_context.Asignaciones.Any(a => a.EnvioId == e.Id))
                .OrderByDescending(e => e.CreadoEn)
                .ToListAsync();
        }

        public async Task<OportunidadesResultadoDto?> ObtenerOportunidadesParaViajeAsync(
            int viajeId, DateTime fechaViaje, int usuarioId, string orden, CancellationToken cancellationToken)
        {
            var viaje = await _context.Viajes.AsNoTracking().Include(v => v.ViajeDias)
                .FirstOrDefaultAsync(v => v.Id == viajeId && v.UsuarioId == usuarioId && v.Activo, cancellationToken);
            if (viaje is null) return null;
            var fecha = fechaViaje.Date;
            if (fecha < DateTime.Today) throw new InvalidOperationException("No se pueden consultar oportunidades para una fecha pasada.");
            var dia = fecha.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fecha.DayOfWeek;
            if (!viaje.ViajeDias.Any(d => d.DiaSemana == dia)) throw new InvalidOperationException("La fecha seleccionada no coincide con los días programados para este viaje.");

            var asignaciones = await _context.Asignaciones.AsNoTracking().Where(a => a.ViajeId == viajeId && a.FechaViaje.Date == fecha).ToListAsync(cancellationToken);
            var idsAsignados = asignaciones.Select(a => a.EnvioId).ToList();
            var pesoAsignado = idsAsignados.Count == 0 ? 0m : await _context.Envios.Where(e => idsAsignados.Contains(e.Id)).SumAsync(e => e.PesoKg, cancellationToken);
            var cupo = Math.Max(0, viaje.MaxPaquetes - asignaciones.Count);
            var peso = Math.Max(0, viaje.PesoMaxKg - pesoAsignado);
            var desvio = Math.Max(0, viaje.DesvioMaxKm - asignaciones.Sum(a => a.DesvioKm));

            var candidatas = await _context.Envios.AsNoTracking().Where(e => e.UsuarioId != usuarioId && e.Estado == (byte)EstadoEnvio.BuscandoConductor && e.FechaLimite.Date >= fecha && !_context.Asignaciones.Any(a => a.EnvioId == e.Id)).ToListAsync(cancellationToken);
            var oportunidades = candidatas
                .Where(e => cupo > 0 && e.PesoKg <= peso && OptimizadorService.EstimarDesvioKm(e.Id) <= desvio)
                .Select(e =>
                {
                    var desvioEstimado = OptimizadorService.EstimarDesvioKm(e.Id);
                    return new OportunidadDto
                    {
                        Id = e.Id,
                        OrigenDir = e.OrigenDir,
                        DestinoDir = e.DestinoDir,
                        Descripcion = e.Descripcion ?? string.Empty,
                        PesoKg = e.PesoKg,
                        Monto = e.Monto,
                        DesvioKm = desvioEstimado,
                        Compatibilidad = ClasificarCompatibilidad(desvioEstimado)
                    };
                });

            oportunidades = orden.ToLowerInvariant() switch
            {
                "desvio" => oportunidades.OrderBy(o => o.DesvioKm).ThenByDescending(o => o.Monto).ThenBy(o => o.Id),
                "compatibilidad" => oportunidades
                    .OrderByDescending(o => o.Monto / (1 + o.DesvioKm))
                    .ThenBy(o => o.DesvioKm)
                    .ThenBy(o => o.Id),
                _ => oportunidades.OrderByDescending(o => o.Monto).ThenBy(o => o.DesvioKm).ThenBy(o => o.Id)
            };

            return new OportunidadesResultadoDto { ViajeId = viaje.Id, FechaViaje = fecha, OrigenDir = viaje.OrigenDir, DestinoDir = viaje.DestinoDir, CupoDisponible = cupo, PesoDisponibleKg = peso, DesvioDisponibleKm = desvio, Oportunidades = oportunidades.ToList() };
        }

        private static string ClasificarCompatibilidad(decimal desvioKm) =>
            desvioKm <= 1 ? "Muy compatible" : desvioKm <= 2 ? "Compatible" : "Mucho desvío";

        // Detalle de un envío visto desde uno de MIS viajes en una fecha concreta.
        // Devuelve null si el viaje no es mío/no está activo o el envío no está disponible.
        public async Task<DetalleOportunidadViajeDto?> ObtenerDetalleOportunidadParaViajeAsync(
            int envioId, int viajeId, DateTime fechaViaje, int usuarioId, CancellationToken cancellationToken)
        {
            // Reutiliza las validaciones de fecha/día y el cálculo de capacidad restante.
            var contexto = await ObtenerOportunidadesParaViajeAsync(viajeId, fechaViaje, usuarioId, "ganancia", cancellationToken);
            if (contexto is null) return null;

            var entidad = await _context.Envios.AsNoTracking().FirstOrDefaultAsync(e => e.Id == envioId
                           && e.UsuarioId != usuarioId
                           && e.Estado == (byte)EstadoEnvio.BuscandoConductor, cancellationToken);
            if (entidad is null) return null;

            var envio = CrearDto(entidad, asignacion: null);

            var viaje = await _context.Viajes.AsNoTracking()
                .FirstAsync(v => v.Id == viajeId, cancellationToken);

            var desvio = OptimizadorService.EstimarDesvioKm(envioId);
            var yaAsignado = await _context.Asignaciones.AnyAsync(a => a.EnvioId == envioId, cancellationToken);

            return new DetalleOportunidadViajeDto
            {
                Envio = envio,
                ViajeId = viaje.Id,
                FechaViaje = contexto.FechaViaje,
                ViajeOrigenDir = viaje.OrigenDir,
                ViajeDestinoDir = viaje.DestinoDir,
                DistanciaHabitualKm = viaje.DistanciaKm,
                DesvioKm = desvio,
                DistanciaConEnvioKm = viaje.DistanciaKm + desvio,
                DuracionHabitualMin = viaje.DuracionMin,
                TiempoAdicionalMin = OptimizadorService.EstimarTiempoExtraMin(desvio),
                CupoRestante = contexto.CupoDisponible,
                PesoRestanteKg = contexto.PesoDisponibleKg,
                PagoPorKmAdicional = desvio > 0 ? Math.Round(envio.Monto / desvio, 2) : envio.Monto,
                Compatibilidad = ClasificarCompatibilidad(desvio),
                PuedeAceptar = !yaAsignado
                               && contexto.CupoDisponible > 0
                               && envio.PesoKg <= contexto.PesoDisponibleKg
                               && desvio <= contexto.DesvioDisponibleKm
            };
        }

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
                PuedeEditar = estadoEnvio is EstadoEnvio.BuscandoConductor,
                PuedeCancelar = estadoEnvio is EstadoEnvio.BuscandoConductor or EstadoEnvio.ConductorAsignado,
                PuedeEliminar = estadoEnvio is EstadoEnvio.Cancelado,
                EstadoNombre = estadoEnvio.ObtenerNombre()
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
