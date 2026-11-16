using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;
using System.Globalization;

namespace RuteaYa.Servicios
{
    public interface IOptimizadorService
    {
        // Calcula la mejor combinación de oportunidades para un viaje en una fecha.
        // Devuelve null si el viaje no existe o no pertenece al usuario.
        Task<OptimizacionDto?> OptimizarAsync(
            int viajeId,
            DateTime fechaViaje,
            int usuarioId,
            CancellationToken cancellationToken = default);

        // Reserva todos los envíos juntos. Devuelve false si el viaje no existe o no es del usuario.
        Task<bool> ReservarCombinacionAsync(
            int viajeId,
            DateTime fechaViaje,
            IReadOnlyCollection<int> enviosIds,
            int usuarioId,
            CancellationToken cancellationToken = default);
    }

    public class OptimizadorService : IOptimizadorService
    {
        // Para que la búsqueda exhaustiva siga siendo rápida (2^20 combinaciones como máximo).
        public const int MaximoCandidatos = 20;
        public const int MinutosExtraPorKm = 3;

        private const int CantidadAlternativas = 2;
        private const int CantidadDescartadas = 2;

        private const string MensajeCombinacionCambio =
            "Otro conductor tomó un envío o ya no está disponible. Recalculá la combinación.";

        private readonly RuteaYaDbContext _context;

        public OptimizadorService(RuteaYaDbContext context)
        {
            _context = context;
        }

        // Si las rutas reales salen más sinuosas que esto, el factor se acota.
        private const decimal FactorSinuosidadPorDefecto = 1.3m;
        private const decimal FactorSinuosidadMaximo = 3m;

        // Estimación de respaldo para datos viejos que guardaron las coordenadas en 0.
        public static decimal EstimarDesvioKm(int envioId)
        {
            return 0.5m * ((envioId % 8) + 1);
        }

        // Desvío real: kilómetros extra que suma el viaje al pasar por el origen y el
        // destino del envío (origen del viaje -> origen del envío -> destino del envío ->
        // destino del viaje), contra ir directo. Se calcula con las coordenadas guardadas
        // y se ajusta con la sinuosidad real del viaje (su distancia por calle contra la
        // distancia en línea recta). Si falta alguna coordenada, usa la estimación.
        public static decimal CalcularDesvioKm(Viaje viaje, Envio envio)
        {
            if (!CalculoGeografico.TieneCoordenadas(viaje.OrigenLat, viaje.OrigenLng)
                || !CalculoGeografico.TieneCoordenadas(viaje.DestinoLat, viaje.DestinoLng)
                || !CalculoGeografico.TieneCoordenadas(envio.OrigenLat, envio.OrigenLng)
                || !CalculoGeografico.TieneCoordenadas(envio.DestinoLat, envio.DestinoLng))
            {
                return EstimarDesvioKm(envio.Id);
            }

            var directa = CalculoGeografico.DistanciaKm(
                viaje.OrigenLat, viaje.OrigenLng, viaje.DestinoLat, viaje.DestinoLng);

            var conEnvio =
                CalculoGeografico.DistanciaKm(
                    viaje.OrigenLat, viaje.OrigenLng, envio.OrigenLat, envio.OrigenLng)
                + CalculoGeografico.DistanciaKm(
                    envio.OrigenLat, envio.OrigenLng, envio.DestinoLat, envio.DestinoLng)
                + CalculoGeografico.DistanciaKm(
                    envio.DestinoLat, envio.DestinoLng, viaje.DestinoLat, viaje.DestinoLng);

            var extraEnLinea = Math.Max(0m, conEnvio - directa);

            return Math.Round(
                extraEnLinea * CalcularSinuosidad(viaje, directa),
                2,
                MidpointRounding.AwayFromZero);
        }

        private static decimal CalcularSinuosidad(Viaje viaje, decimal distanciaDirecta)
        {
            if (distanciaDirecta <= 0m || viaje.DistanciaKm <= 0m)
            {
                return FactorSinuosidadPorDefecto;
            }

            var factor = viaje.DistanciaKm / distanciaDirecta;

            return Math.Min(FactorSinuosidadMaximo, Math.Max(1m, factor));
        }

        public static int EstimarTiempoExtraMin(decimal desvioKm)
        {
            return (int)Math.Round(desvioKm * MinutosExtraPorKm, MidpointRounding.AwayFromZero);
        }

        public async Task<OptimizacionDto?> OptimizarAsync(
            int viajeId,
            DateTime fechaViaje,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            var disponibilidad = await ObtenerDisponibilidadAsync(
                viajeId, fechaViaje, usuarioId, cancellationToken);

            if (disponibilidad == null)
            {
                return null;
            }

            var dia = fechaViaje.Date;

            // Oportunidades reales: de otros usuarios, sin conductor, sin asignar y a tiempo para la fecha.
            var oportunidades = await _context.Envios
                .AsNoTracking()
                .Where(e => e.UsuarioId != usuarioId
                         && e.Estado == (byte)EstadoEnvio.BuscandoConductor
                         && e.FechaLimite >= dia
                         && !_context.Asignaciones.Any(a => a.EnvioId == e.Id))
                .OrderByDescending(e => e.Monto)
                .ThenBy(e => e.Id)
                .ToListAsync(cancellationToken);

            // Solo entran los que, por sí solos, caben en el peso y el desvío disponibles.
            var pool = oportunidades
                .Where(e => e.PesoKg <= disponibilidad.PesoKg
                         && CalcularDesvioKm(disponibilidad.Viaje, e) <= disponibilidad.DesvioKm)
                .Take(MaximoCandidatos)
                .ToList();

            var (validas, descartadas) = Buscar(
                pool, disponibilidad.Viaje, disponibilidad.Cupo, disponibilidad.PesoKg, disponibilidad.DesvioKm);

            return new OptimizacionDto
            {
                ViajeId = disponibilidad.Viaje.Id,
                OrigenDir = disponibilidad.Viaje.OrigenDir,
                DestinoDir = disponibilidad.Viaje.DestinoDir,
                FechaViaje = dia,
                CupoDisponible = disponibilidad.Cupo,
                PesoDisponibleKg = disponibilidad.PesoKg,
                DesvioDisponibleKm = disponibilidad.DesvioKm,
                CandidatosEvaluados = pool.Count,
                Recomendada = validas.Count > 0
                    ? CrearCombinacion(pool, validas[0], true, disponibilidad)
                    : null,
                Alternativas = validas
                    .Skip(1)
                    .Select(s => CrearCombinacion(pool, s, true, disponibilidad))
                    .ToList(),
                Descartadas = descartadas
                    .Select(s => CrearCombinacion(pool, s, false, disponibilidad))
                    .ToList()
            };
        }

        public async Task<bool> ReservarCombinacionAsync(
            int viajeId,
            DateTime fechaViaje,
            IReadOnlyCollection<int> enviosIds,
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            if (enviosIds == null || enviosIds.Count == 0)
            {
                throw new InvalidOperationException("Elegí al menos un envío para reservar.");
            }

            var ids = enviosIds.Distinct().ToList();
            if (ids.Count != enviosIds.Count)
            {
                throw new InvalidOperationException("La combinación tiene envíos repetidos.");
            }

            var disponibilidad = await ObtenerDisponibilidadAsync(
                viajeId, fechaViaje, usuarioId, cancellationToken);

            if (disponibilidad == null)
            {
                return false;
            }

            var dia = fechaViaje.Date;

            var envios = await _context.Envios
                .Where(e => ids.Contains(e.Id))
                .ToListAsync(cancellationToken);

            if (envios.Any(e => e.UsuarioId == usuarioId))
            {
                throw new InvalidOperationException("No podés reservar tus propios envíos.");
            }

            var yaAsignados = await _context.Asignaciones
                .AnyAsync(a => ids.Contains(a.EnvioId), cancellationToken);

            // Si otro conductor los tomó mientras se miraba la pantalla, se pide recalcular.
            if (envios.Count != ids.Count
                || yaAsignados
                || envios.Any(e => e.Estado != (byte)EstadoEnvio.BuscandoConductor
                                || e.FechaLimite.Date < dia))
            {
                throw new InvalidOperationException(MensajeCombinacionCambio);
            }

            // La API es la autoridad: se vuelven a validar los límites del viaje.
            if (envios.Count > disponibilidad.Cupo)
            {
                throw new InvalidOperationException(
                    "La combinación supera la cantidad de paquetes disponibles del viaje.");
            }

            if (envios.Sum(e => e.PesoKg) > disponibilidad.PesoKg)
            {
                throw new InvalidOperationException(
                    $"La combinación supera el peso disponible del viaje ({Formatear(disponibilidad.PesoKg)} kg).");
            }

            if (envios.Sum(e => CalcularDesvioKm(disponibilidad.Viaje, e)) > disponibilidad.DesvioKm)
            {
                throw new InvalidOperationException(
                    $"La combinación supera el desvío disponible del viaje ({Formatear(disponibilidad.DesvioKm)} km).");
            }

            var ahora = DateTime.Now;
            foreach (var envio in envios)
            {
                var desvio = CalcularDesvioKm(disponibilidad.Viaje, envio);

                _context.Asignaciones.Add(new Asignacion
                {
                    EnvioId = envio.Id,
                    ViajeId = viajeId,
                    FechaViaje = dia,
                    DesvioKm = desvio,
                    TiempoExtraMin = EstimarTiempoExtraMin(desvio),
                    Ganancia = envio.Monto,
                    AsignadoEn = ahora
                });

                var estadoAnterior = envio.Estado;
                envio.Estado = (byte)EstadoEnvio.ConductorAsignado;

                _context.EnvioEstadoHistoriales.Add(new EnvioEstadoHistorial
                {
                    EnvioId = envio.Id,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = envio.Estado,
                    Fecha = ahora
                });
            }

            try
            {
                // Un solo SaveChanges: se reservan todos o ninguno.
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // RowVersion: otro conductor modificó un envío en el medio.
                throw new InvalidOperationException(MensajeCombinacionCambio);
            }

            return true;
        }

        // ---------- Capacidad del viaje ----------

        private sealed record Disponibilidad(Viaje Viaje, int Cupo, decimal PesoKg, decimal DesvioKm);

        // Mismas reglas que VincularEnvioAViajeAsync, más lo que ya está asignado ese día.
        private async Task<Disponibilidad?> ObtenerDisponibilidadAsync(
            int viajeId,
            DateTime fechaViaje,
            int usuarioId,
            CancellationToken cancellationToken)
        {
            var viaje = await _context.Viajes
                .AsNoTracking()
                .Include(v => v.ViajeDias)
                .FirstOrDefaultAsync(v => v.Id == viajeId && v.UsuarioId == usuarioId, cancellationToken);

            if (viaje == null)
            {
                return null;
            }

            if (!viaje.Activo)
            {
                throw new InvalidOperationException("El viaje no se encuentra activo.");
            }

            var dia = fechaViaje.Date;

            if (dia < DateTime.Today)
            {
                throw new InvalidOperationException("No se puede optimizar para una fecha pasada.");
            }

            byte diaSemana = dia.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)dia.DayOfWeek;
            if (!viaje.ViajeDias.Any(d => d.DiaSemana == diaSemana))
            {
                throw new InvalidOperationException(
                    "La fecha seleccionada no coincide con los días programados para este viaje.");
            }

            var asignaciones = await _context.Asignaciones
                .AsNoTracking()
                .Where(a => a.ViajeId == viajeId && a.FechaViaje.Date == dia)
                .Select(a => new { a.EnvioId, a.DesvioKm })
                .ToListAsync(cancellationToken);

            var idsAsignados = asignaciones.Select(a => a.EnvioId).ToList();
            var pesoAsignado = idsAsignados.Count == 0
                ? 0m
                : await _context.Envios
                    .Where(e => idsAsignados.Contains(e.Id))
                    .SumAsync(e => e.PesoKg, cancellationToken);

            return new Disponibilidad(
                viaje,
                Math.Max(0, viaje.MaxPaquetes - asignaciones.Count),
                Math.Max(0m, viaje.PesoMaxKg - pesoAsignado),
                Math.Max(0m, viaje.DesvioMaxKm - asignaciones.Sum(a => a.DesvioKm)));
        }

        // ---------- Búsqueda de la mejor combinación ----------

        private readonly record struct Subconjunto(
            int Mascara, int Cantidad, decimal Ganancia, decimal Peso, decimal Desvio);

        // Mayor ganancia primero; en empate, menor desvío; luego menor peso y menos paquetes.
        private static int Comparar(Subconjunto a, Subconjunto b)
        {
            var c = b.Ganancia.CompareTo(a.Ganancia);
            if (c != 0) return c;

            c = a.Desvio.CompareTo(b.Desvio);
            if (c != 0) return c;

            c = a.Peso.CompareTo(b.Peso);
            if (c != 0) return c;

            c = a.Cantidad.CompareTo(b.Cantidad);
            if (c != 0) return c;

            return a.Mascara.CompareTo(b.Mascara);
        }

        // Recorre todos los subconjuntos de hasta "cupo" envíos y se queda con los mejores.
        private static (List<Subconjunto> Validas, List<Subconjunto> Descartadas) Buscar(
            IReadOnlyList<Envio> pool,
            Viaje viaje,
            int cupo,
            decimal pesoMaximo,
            decimal desvioMaximo)
        {
            var desvios = pool.Select(e => CalcularDesvioKm(viaje, e)).ToArray();
            var validas = new List<Subconjunto>();
            var descartadas = new List<Subconjunto>();
            var limiteValidas = 1 + CantidadAlternativas;

            static void Registrar(List<Subconjunto> lista, int limite, Subconjunto candidato)
            {
                var posicion = lista.FindIndex(x => Comparar(candidato, x) < 0);

                if (posicion < 0)
                {
                    if (lista.Count < limite)
                    {
                        lista.Add(candidato);
                    }
                    return;
                }

                lista.Insert(posicion, candidato);
                if (lista.Count > limite)
                {
                    lista.RemoveAt(lista.Count - 1);
                }
            }

            void Explorar(int inicio, int mascara, int cantidad, decimal ganancia, decimal peso, decimal desvio)
            {
                for (var i = inicio; i < pool.Count; i++)
                {
                    var actual = new Subconjunto(
                        mascara | (1 << i),
                        cantidad + 1,
                        ganancia + pool[i].Monto,
                        peso + pool[i].PesoKg,
                        desvio + desvios[i]);

                    if (actual.Peso <= pesoMaximo && actual.Desvio <= desvioMaximo)
                    {
                        Registrar(validas, limiteValidas, actual);
                    }
                    else
                    {
                        Registrar(descartadas, CantidadDescartadas, actual);
                    }

                    if (actual.Cantidad < cupo)
                    {
                        Explorar(i + 1, actual.Mascara, actual.Cantidad, actual.Ganancia, actual.Peso, actual.Desvio);
                    }
                }
            }

            if (cupo > 0 && pool.Count > 0)
            {
                Explorar(0, 0, 0, 0m, 0m, 0m);
            }

            return (validas, descartadas);
        }

        private static CombinacionDto CrearCombinacion(
            IReadOnlyList<Envio> pool,
            Subconjunto subconjunto,
            bool esValida,
            Disponibilidad disponibilidad)
        {
            var paquetes = new List<PaqueteOptimizableDto>();
            for (var i = 0; i < pool.Count; i++)
            {
                if ((subconjunto.Mascara & (1 << i)) == 0)
                {
                    continue;
                }

                var envio = pool[i];
                var desvio = CalcularDesvioKm(disponibilidad.Viaje, envio);
                paquetes.Add(new PaqueteOptimizableDto
                {
                    Id = envio.Id,
                    OrigenDir = envio.OrigenDir,
                    DestinoDir = envio.DestinoDir,
                    Descripcion = envio.Descripcion ?? string.Empty,
                    PesoKg = envio.PesoKg,
                    Monto = envio.Monto,
                    DesvioKm = desvio,
                    TiempoExtraMin = EstimarTiempoExtraMin(desvio),
                    FechaLimite = envio.FechaLimite
                });
            }

            string? motivo = null;
            if (!esValida)
            {
                motivo = subconjunto.Peso > disponibilidad.PesoKg
                    ? $"Supera {Formatear(disponibilidad.PesoKg)} kg"
                    : $"Supera {Formatear(disponibilidad.DesvioKm)} km";
            }

            return new CombinacionDto
            {
                Paquetes = paquetes,
                GananciaTotal = subconjunto.Ganancia,
                PesoTotalKg = subconjunto.Peso,
                DesvioTotalKm = subconjunto.Desvio,
                EsValida = esValida,
                MotivoRechazo = motivo
            };
        }

        private static string Formatear(decimal valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
