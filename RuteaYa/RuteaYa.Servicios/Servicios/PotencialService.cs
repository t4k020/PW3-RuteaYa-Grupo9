using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Servicios;

public interface IPotencialService
{
    Task<PotencialMensualDto> CalcularAsync(int usuarioId, int anio, int mes, string criterio, CancellationToken cancellationToken = default);
}

public sealed class PotencialService : IPotencialService
{
    private readonly RuteaYaDbContext _context;
    public PotencialService(RuteaYaDbContext context) => _context = context;

    public async Task<PotencialMensualDto> CalcularAsync(int usuarioId, int anio, int mes, string criterio, CancellationToken cancellationToken = default)
    {
        if (mes is < 1 or > 12) throw new InvalidOperationException("El mes no es válido.");
        criterio = criterio.ToLowerInvariant() switch { "desvio" => "desvio", "equilibrio" => "equilibrio", _ => "ganancia" };
        var inicio = new DateTime(anio, mes, 1);
        var fin = inicio.AddMonths(1);
        var desde = DateTime.Today > inicio ? DateTime.Today : inicio;
        var viajes = await _context.Viajes.AsNoTracking().Include(v => v.ViajeDias)
            .Where(v => v.UsuarioId == usuarioId && v.Activo).ToListAsync(cancellationToken);
        var envios = await _context.Envios.AsNoTracking()
            .Where(e => e.UsuarioId != usuarioId && e.Estado == (byte)EstadoEnvio.BuscandoConductor && e.FechaLimite >= desde && e.FechaLimite < fin && !_context.Asignaciones.Any(a => a.EnvioId == e.Id))
            .OrderByDescending(e => e.Monto).Take(OptimizadorService.MaximoCandidatos).ToListAsync(cancellationToken);
        var usados = new HashSet<int>();
        var resultado = new PotencialMensualDto { Anio = anio, Mes = mes, Criterio = criterio, Semanas = Enumerable.Range(1, 6).Select(n => new PotencialSemanaDto { Numero = n }).ToList() };

        for (var fecha = desde; fecha < fin; fecha = fecha.AddDays(1))
        foreach (var viaje in viajes.Where(v => v.ViajeDias.Any(d => d.DiaSemana == Dia(fecha))))
        {
            var candidatos = envios.Where(e => !usados.Contains(e.Id) && e.FechaLimite.Date >= fecha.Date && e.PesoKg <= viaje.PesoMaxKg && OptimizadorService.EstimarDesvioKm(e.Id) <= viaje.DesvioMaxKm).ToList();
            var seleccion = Elegir(candidatos, viaje.MaxPaquetes, viaje.PesoMaxKg, viaje.DesvioMaxKm, criterio);
            if (seleccion.Count == 0) continue;
            foreach (var envio in seleccion) usados.Add(envio.Id);
            var ganancia = seleccion.Sum(e => e.Monto);
            var desvio = seleccion.Sum(e => OptimizadorService.EstimarDesvioKm(e.Id));
            resultado.Viajes.Add(new PotencialViajeDto { ViajeId = viaje.Id, Fecha = fecha, OrigenDir = viaje.OrigenDir, DestinoDir = viaje.DestinoDir, Paquetes = seleccion.Count, Ganancia = ganancia, DesvioKm = desvio });
            resultado.GananciaPotencial += ganancia;
            resultado.PaquetesPosibles += seleccion.Count;
            resultado.Semanas[Math.Min(5, (fecha.Day - 1) / 7)].Ganancia += ganancia;
        }

        resultado.ViajesAnalizados = resultado.Viajes.Count;
        resultado.PromedioPorViaje = resultado.ViajesAnalizados == 0 ? 0 : resultado.GananciaPotencial / resultado.ViajesAnalizados;
        resultado.GananciaEfectiva = await _context.Asignaciones.AsNoTracking().Include(a => a.Viaje)
            .Where(a => a.Viaje!.UsuarioId == usuarioId && a.FechaViaje >= inicio && a.FechaViaje < fin).SumAsync(a => (decimal?)a.Ganancia, cancellationToken) ?? 0m;
        return resultado;
    }

    private static byte Dia(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fecha.DayOfWeek;

    private static List<Envio> Elegir(List<Envio> candidatos, int cupo, decimal pesoMaximo, decimal desvioMaximo, string criterio)
    {
        List<Envio> mejor = new();
        void Buscar(int desde, List<Envio> actual, decimal peso, decimal desvio)
        {
            if (actual.Count > 0 && Mejor(actual, mejor, criterio)) mejor = actual.ToList();
            if (actual.Count == cupo) return;
            for (var i = desde; i < candidatos.Count; i++)
            {
                var envio = candidatos[i]; var nuevoPeso = peso + envio.PesoKg; var nuevoDesvio = desvio + OptimizadorService.EstimarDesvioKm(envio.Id);
                if (nuevoPeso <= pesoMaximo && nuevoDesvio <= desvioMaximo) { actual.Add(envio); Buscar(i + 1, actual, nuevoPeso, nuevoDesvio); actual.RemoveAt(actual.Count - 1); }
            }
        }
        Buscar(0, new(), 0, 0); return mejor;
    }

    private static bool Mejor(List<Envio> candidata, List<Envio> actual, string criterio)
    {
        if (actual.Count == 0) return true;
        var gc = candidata.Sum(e => e.Monto); var ga = actual.Sum(e => e.Monto);
        var dc = candidata.Sum(e => OptimizadorService.EstimarDesvioKm(e.Id)); var da = actual.Sum(e => OptimizadorService.EstimarDesvioKm(e.Id));
        return criterio switch
        {
            "desvio" => dc < da || (dc == da && gc > ga),
            "equilibrio" => gc / dc > ga / da || (gc / dc == ga / da && gc > ga),
            _ => gc > ga || (gc == ga && dc < da)
        };
    }
}
