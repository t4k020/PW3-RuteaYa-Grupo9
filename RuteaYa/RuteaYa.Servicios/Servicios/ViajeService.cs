using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios.Rutas;
using System.Globalization;

namespace RuteaYa.Servicios
{
    public interface IViajeService
    {
        Task<Viaje> CrearViajeAsync(
            CrearViajeDto solicitud,
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<Viaje?> ObtenerPorIdAsync(int id);
        Task<bool> ActualizarViajeAsync(
            int id,
            CrearViajeDto solicitud,
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<bool> EliminarViajeAsync(int id, int usuarioId);
        Task<IEnumerable<Viaje>> ObtenerTodosAsync();
        Task<IEnumerable<ViajeListadoDto>> ObtenerParaListadoAsync(bool? soloActivo, int usuarioId);
        Task<bool> DesactivarViajeAsync(int id, int usuarioId);
        Task<bool> ActivarViajeAsync(int id, int usuarioId);
        Task<ViajeDetalleDto?> ObtenerDetallePorIdAsync(int id, int usuarioId);
        Task<RecorridoViajeDto?> ObtenerRecorridoAsync(
            int viajeId,
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<RecorridoViajeDto> PrevisualizarRecorridoAsync(
            CrearViajeDto solicitud,
            CancellationToken cancellationToken = default);
        Task<PaqueteDetalleDto?> ObtenerPaqueteAsignadoAsync(
            int viajeId,
            int envioId,
            int usuarioId,
            CancellationToken cancellationToken = default);
        Task<List<Envio>> ObtenerTransportesAsignadosAsync(int usuarioId);
        Task<bool> CambiarEstadoTransporteAsync(int envioId, string accion, int usuarioId);
    }

    public class ViajeService : IViajeService
    {
        private readonly RuteaYaDbContext _context;
        private readonly IRutaService _rutaService;
        private readonly IMemoryCache? _cache;

        // La geometría se calcula on-demand; se guarda unos minutos para no
        // gastar cuota del proveedor en cada visita al detalle.
        private const int MinutosCacheRecorrido = 10;

        public ViajeService(
            RuteaYaDbContext context,
            IRutaService rutaService,
            IMemoryCache? cache = null)
        {
            _context = context;
            _rutaService = rutaService;
            _cache = cache;
        }

        public async Task<Viaje> CrearViajeAsync(
            CrearViajeDto solicitud,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            // Las coordenadas y las distancias las define el servidor a partir
            // de las direcciones: nunca se aceptan desde el cliente.
            var recorrido = await PrepararRecorridoAsync(solicitud, cancellationToken);

            var viaje = new Viaje
            {
                UsuarioId = usuarioId,
                HoraSalida = solicitud.HoraSalida
                    ?? throw new InvalidOperationException("La hora de salida es obligatoria."),
                HoraLlegada = solicitud.HoraLlegada
                    ?? throw new InvalidOperationException("La hora de llegada es obligatoria."),
                MaxPaquetes = solicitud.MaxPaquetes,
                PesoMaxKg = solicitud.PesoMaxKg,
                DesvioMaxKm = solicitud.DesvioMaxKm,
                Activo = true,
                ViajeDias = CrearDias(solicitud)
            };

            AplicarRecorrido(viaje, recorrido);

            _context.Viajes.Add(viaje);
            await _context.SaveChangesAsync(cancellationToken);

            return viaje;
        }

        // Geocodifica origen, paradas y destino, y calcula la ruta que los une
        // en el orden del recorrido. Un fallo de geocodificación es un dato
        // inválido del usuario, por eso se informa como InvalidOperationException.
        private async Task<DatosRecorrido> PrepararRecorridoAsync(
            CrearViajeDto solicitud,
            CancellationToken cancellationToken)
        {
            var origenDir = solicitud.OrigenDir.Trim();
            var destinoDir = solicitud.DestinoDir.Trim();

            var origen = await GeocodificarAsync(origenDir, "origen", cancellationToken);
            var destino = await GeocodificarAsync(destinoDir, "destino", cancellationToken);

            var paradas = new List<ParadaGeocodificada>();

            foreach (var parada in solicitud.Paradas ?? new List<ParadaViajeDto>())
            {
                var direccion = parada.Direccion.Trim();
                var coordenada = await GeocodificarAsync(direccion, "la parada", cancellationToken);
                paradas.Add(new ParadaGeocodificada(direccion, coordenada));
            }

            var puntos = new List<CoordenadaDto> { origen };
            puntos.AddRange(paradas.Select(parada => parada.Coordenada));
            puntos.Add(destino);

            var ruta = await _rutaService.CalcularRutaAsync(puntos, cancellationToken);

            return new DatosRecorrido(origenDir, origen, destinoDir, destino, paradas, ruta);
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

        private static List<ViajeDia> CrearDias(CrearViajeDto solicitud)
        {
            var dias = solicitud.DiasSemana ?? new List<byte>();

            return dias
                .Distinct()
                .OrderBy(dia => dia)
                .Select(dia => new ViajeDia { DiaSemana = dia })
                .ToList();
        }

        private static void AplicarRecorrido(Viaje viaje, DatosRecorrido recorrido)
        {
            viaje.OrigenDir = recorrido.OrigenDir;
            viaje.OrigenLat = recorrido.Origen.Lat;
            viaje.OrigenLng = recorrido.Origen.Lng;
            viaje.DestinoDir = recorrido.DestinoDir;
            viaje.DestinoLat = recorrido.Destino.Lat;
            viaje.DestinoLng = recorrido.Destino.Lng;
            viaje.DistanciaKm = recorrido.Ruta.DistanciaKm;
            viaje.DuracionMin = recorrido.Ruta.DuracionMin;

            // Las paradas se renumeran según el orden en que llegaron.
            viaje.Paradas.Clear();

            for (var indice = 0; indice < recorrido.Paradas.Count; indice++)
            {
                var parada = recorrido.Paradas[indice];

                viaje.Paradas.Add(new Parada
                {
                    Orden = indice + 1,
                    Direccion = parada.Direccion,
                    Lat = parada.Coordenada.Lat,
                    Lng = parada.Coordenada.Lng
                });
            }
        }

        private sealed record ParadaGeocodificada(string Direccion, CoordenadaDto Coordenada);

        private sealed record DatosRecorrido(
            string OrigenDir,
            CoordenadaDto Origen,
            string DestinoDir,
            CoordenadaDto Destino,
            IReadOnlyList<ParadaGeocodificada> Paradas,
            RutaDto Ruta);

        public async Task<Viaje?> ObtenerPorIdAsync(int id)
        {
            return await _context.Viajes
                .Include(v => v.Usuario)
                .Include(v => v.Paradas)
                .Include(v => v.ViajeDias)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<bool> ActualizarViajeAsync(
            int id,
            CrearViajeDto solicitud,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var viajeExistente = await _context.Viajes
                .Include(v => v.Paradas)
                .Include(v => v.ViajeDias)
                .FirstOrDefaultAsync(
                    v => v.Id == id && v.UsuarioId == usuarioId,
                    cancellationToken);

            if (viajeExistente == null)
                return false;

            // Se recalcula todo el recorrido porque origen, destino o paradas
            // pudieron cambiar y las distancias guardadas quedarían obsoletas.
            var recorrido = await PrepararRecorridoAsync(solicitud, cancellationToken);

            viajeExistente.HoraSalida = solicitud.HoraSalida
                ?? throw new InvalidOperationException("La hora de salida es obligatoria.");
            viajeExistente.HoraLlegada = solicitud.HoraLlegada
                ?? throw new InvalidOperationException("La hora de llegada es obligatoria.");
            viajeExistente.MaxPaquetes = solicitud.MaxPaquetes;
            viajeExistente.PesoMaxKg = solicitud.PesoMaxKg;
            viajeExistente.DesvioMaxKm = solicitud.DesvioMaxKm;

            AplicarRecorrido(viajeExistente, recorrido);
            ActualizarDias(viajeExistente, solicitud);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        // Sincroniza los días sin borrar y volver a insertar los que se repiten:
        // la clave de ViajeDia es compuesta (ViajeId + DiaSemana) y EF no puede
        // seguir dos instancias con la misma clave en el mismo SaveChanges.
        private static void ActualizarDias(Viaje viaje, CrearViajeDto solicitud)
        {
            var solicitados = (solicitud.DiasSemana ?? new List<byte>()).Distinct().ToHashSet();

            foreach (var dia in viaje.ViajeDias.Where(dia => !solicitados.Contains(dia.DiaSemana)).ToList())
            {
                viaje.ViajeDias.Remove(dia);
            }

            var existentes = viaje.ViajeDias.Select(dia => dia.DiaSemana).ToHashSet();

            foreach (var dia in solicitados.Where(dia => !existentes.Contains(dia)).OrderBy(dia => dia))
            {
                viaje.ViajeDias.Add(new ViajeDia { DiaSemana = dia });
            }
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

                DiasSemanaNumeros = viaje.ViajeDias
                    .OrderBy(vd => vd.DiaSemana)
                    .Select(vd => vd.DiaSemana)
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

        public async Task<RecorridoViajeDto?> ObtenerRecorridoAsync(
            int viajeId,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var viaje = await _context.Viajes
                .AsNoTracking()
                .Include(v => v.Paradas)
                .FirstOrDefaultAsync(
                    v => v.Id == viajeId && v.UsuarioId == usuarioId,
                    cancellationToken);

            if (viaje is null)
            {
                return null;
            }

            var paradas = viaje.Paradas.OrderBy(p => p.Orden).ToList();

            var puntos = new List<PuntoRecorridoDto>
            {
                new()
                {
                    Orden = 0,
                    Direccion = viaje.OrigenDir,
                    Lat = viaje.OrigenLat,
                    Lng = viaje.OrigenLng
                }
            };

            puntos.AddRange(paradas.Select(parada => new PuntoRecorridoDto
            {
                Orden = parada.Orden,
                Direccion = parada.Direccion,
                Lat = parada.Lat,
                Lng = parada.Lng
            }));

            puntos.Add(new PuntoRecorridoDto
            {
                Orden = paradas.Count + 1,
                Direccion = viaje.DestinoDir,
                Lat = viaje.DestinoLat,
                Lng = viaje.DestinoLng
            });

            var coordenadas = puntos
                .Select(punto => new CoordenadaDto(punto.Lat, punto.Lng))
                .ToList();

            // La clave incluye las coordenadas: si el viaje cambia de recorrido
            // se calcula una geometría nueva sin necesidad de invalidar nada.
            var clave = CrearClaveRecorrido(viaje.Id, coordenadas);

            if (_cache is not null
                && _cache.TryGetValue(clave, out RecorridoViajeDto? cacheado)
                && cacheado is not null)
            {
                return cacheado;
            }

            var ruta = await _rutaService.CalcularRutaAsync(coordenadas, cancellationToken);

            var recorrido = new RecorridoViajeDto
            {
                ViajeId = viaje.Id,
                Puntos = puntos,
                // Si el proveedor no devuelve puntos intermedios se une cada
                // parada con una línea recta: el mapa sigue siendo útil.
                Geometria = (ruta.Geometria ?? coordenadas).ToList(),
                DistanciaKm = ruta.DistanciaKm,
                DuracionMin = ruta.DuracionMin,
                Fuente = _rutaService.NombreProveedor
            };

            _cache?.Set(clave, recorrido, TimeSpan.FromMinutes(MinutosCacheRecorrido));

            return recorrido;
        }

        // Calcula el recorrido sin guardarlo. Se usa al crear un viaje, antes
        // de que exista el registro, para mostrar la previsualización del mapa.
        public async Task<RecorridoViajeDto> PrevisualizarRecorridoAsync(
            CrearViajeDto solicitud,
            CancellationToken cancellationToken = default)
        {
            var recorrido = await PrepararRecorridoAsync(solicitud, cancellationToken);

            var puntos = new List<PuntoRecorridoDto>
            {
                new()
                {
                    Orden = 0,
                    Direccion = recorrido.OrigenDir,
                    Lat = recorrido.Origen.Lat,
                    Lng = recorrido.Origen.Lng
                }
            };

            puntos.AddRange(recorrido.Paradas.Select((parada, indice) => new PuntoRecorridoDto
            {
                Orden = indice + 1,
                Direccion = parada.Direccion,
                Lat = parada.Coordenada.Lat,
                Lng = parada.Coordenada.Lng
            }));

            puntos.Add(new PuntoRecorridoDto
            {
                Orden = recorrido.Paradas.Count + 1,
                Direccion = recorrido.DestinoDir,
                Lat = recorrido.Destino.Lat,
                Lng = recorrido.Destino.Lng
            });

            var coordenadas = puntos
                .Select(punto => new CoordenadaDto(punto.Lat, punto.Lng))
                .ToList();

            return new RecorridoViajeDto
            {
                Puntos = puntos,
                Geometria = (recorrido.Ruta.Geometria ?? coordenadas).ToList(),
                DistanciaKm = recorrido.Ruta.DistanciaKm,
                DuracionMin = recorrido.Ruta.DuracionMin,
                Fuente = _rutaService.NombreProveedor
            };
        }

        private static string CrearClaveRecorrido(
            int viajeId,
            IReadOnlyList<CoordenadaDto> puntos)
        {
            var coordenadas = puntos.Select(punto =>
                $"{punto.Lat.ToString(CultureInfo.InvariantCulture)}," +
                $"{punto.Lng.ToString(CultureInfo.InvariantCulture)}");

            return $"recorrido:{viajeId}:{string.Join(';', coordenadas)}";
        }

        public async Task<PaqueteDetalleDto?> ObtenerPaqueteAsignadoAsync(
            int viajeId,
            int envioId,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            // La consulta valida en un solo paso que el viaje sea del usuario y que
            // el envío esté realmente asignado a ese viaje. Si algo no coincide,
            // devolvemos null para que la API responda 404 sin revelar información.
            var paquete = await (
                from asignacion in _context.Asignaciones.AsNoTracking()
                join viaje in _context.Viajes on asignacion.ViajeId equals viaje.Id
                join envio in _context.Envios on asignacion.EnvioId equals envio.Id
                join usuario in _context.Usuarios on envio.UsuarioId equals usuario.Id
                where asignacion.ViajeId == viajeId
                      && asignacion.EnvioId == envioId
                      && viaje.UsuarioId == usuarioId
                select new PaqueteDetalleDto
                {
                    EnvioId = envio.Id,
                    ViajeId = viaje.Id,
                    Descripcion = envio.Descripcion ?? string.Empty,
                    Estado = envio.Estado,
                    FechaLimite = envio.FechaLimite,
                    Monto = envio.Monto,
                    PesoKg = envio.PesoKg,
                    Tamano = envio.Tamano,
                    OrigenDir = envio.OrigenDir,
                    DestinoDir = envio.DestinoDir,
                    CreadoEn = envio.CreadoEn,
                    NombreRemitente = usuario.Nombre + " " + usuario.Apellido,
                    FechaViaje = asignacion.FechaViaje,
                    DesvioKm = asignacion.DesvioKm,
                    TiempoExtraMin = asignacion.TiempoExtraMin,
                    Ganancia = asignacion.Ganancia
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (paquete is null)
            {
                return null;
            }

            // Los nombres legibles se calculan en memoria para no depender de
            // funciones que el proveedor de base de datos no sepa traducir.
            paquete.EstadoNombre = ((EstadoEnvio)paquete.Estado).ObtenerNombre();
            paquete.TamanoNombre = ObtenerNombreTamano(paquete.Tamano);
            paquete.NombreRemitente = paquete.NombreRemitente.Trim();

            return paquete;
        }

        private static string ObtenerNombreTamano(byte tamano) => tamano switch
        {
            1 => "Pequeño",
            2 => "Mediano",
            3 => "Grande",
            _ => "No especificado"
        };

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
