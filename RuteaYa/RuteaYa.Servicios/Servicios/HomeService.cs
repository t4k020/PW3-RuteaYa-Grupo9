using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Servicios;

public interface IHomeService
{
    Task<HomeDashboardDto> ObtenerDashboardAsync(int usuarioId, DateTime fechaActual, CancellationToken cancellationToken);
}

public class HomeService : IHomeService
{
    private readonly RuteaYaDbContext _context;

    public HomeService(RuteaYaDbContext context)
    {
        _context = context;
    }

    public async Task<HomeDashboardDto> ObtenerDashboardAsync(
        int usuarioId,
        DateTime fechaActual,
        CancellationToken cancellationToken)
    {
        // Se usa el rango del mes para calcular las ganancias del período actual.
        var inicioMes = new DateTime(fechaActual.Year, fechaActual.Month, 1);
        var inicioProximoMes = inicioMes.AddMonths(1);

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.Nombre)
            .SingleOrDefaultAsync(cancellationToken);

        if (usuario is null)
        {
            throw new KeyNotFoundException("No se encontró el usuario autenticado.");
        }

        // Las cantidades se calculan en la base y no sobre los cinco elementos visibles.
        var cantidadViajes = await _context.Viajes
            .AsNoTracking()
            .CountAsync(v => v.UsuarioId == usuarioId && v.Activo, cancellationToken);

        var cantidadEnvios = await _context.Envios
            .AsNoTracking()
            .CountAsync(e =>
                e.UsuarioId == usuarioId &&
                e.Estado != (byte)EstadoEnvio.Entregado &&
                e.Estado != (byte)EstadoEnvio.Cancelado,
                cancellationToken);

        var cantidadOportunidades = await _context.Envios
            .AsNoTracking()
            .CountAsync(e =>
                e.UsuarioId != usuarioId &&
                e.Estado == (byte)EstadoEnvio.BuscandoConductor &&
                e.FechaLimite >= fechaActual.Date &&
                !_context.Asignaciones.Any(a => a.EnvioId == e.Id),
                cancellationToken);

        var viajes = await _context.Viajes
            .AsNoTracking()
            .Where(v => v.UsuarioId == usuarioId && v.Activo)
            .Include(v => v.ViajeDias)
            .OrderBy(v => v.HoraSalida)
            .Take(5)
            .ToListAsync(cancellationToken);

        var envios = await _context.Envios
            .AsNoTracking()
            .Where(e =>
                e.UsuarioId == usuarioId &&
                e.Estado != (byte)EstadoEnvio.Entregado &&
                e.Estado != (byte)EstadoEnvio.Cancelado)
            .OrderBy(e => e.FechaLimite)
            .Take(5)
            .ToListAsync(cancellationToken);

        var oportunidades = await _context.Envios
            .AsNoTracking()
            .Where(e =>
                e.UsuarioId != usuarioId &&
                e.Estado == (byte)EstadoEnvio.BuscandoConductor &&
                e.FechaLimite >= fechaActual.Date &&
                !_context.Asignaciones.Any(a => a.EnvioId == e.Id))
            .OrderBy(e => e.FechaLimite)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Ganancia efectiva: asignaciones ya registradas en viajes del usuario.
        var gananciaEfectiva = await _context.Asignaciones
            .AsNoTracking()
            .Where(a =>
                a.Viaje!.UsuarioId == usuarioId &&
                a.FechaViaje >= inicioMes &&
                a.FechaViaje < inicioProximoMes)
            .SumAsync(a => (decimal?)a.Ganancia, cancellationToken) ?? 0m;

        var potencialOportunidades = await _context.Envios
            .AsNoTracking()
            .Where(e =>
                e.UsuarioId != usuarioId &&
                e.Estado == (byte)EstadoEnvio.BuscandoConductor &&
                e.FechaLimite >= inicioMes &&
                e.FechaLimite < inicioProximoMes &&
                !_context.Asignaciones.Any(a => a.EnvioId == e.Id))
            .SumAsync(e => (decimal?)e.Monto, cancellationToken) ?? 0m;

        // Se transforma la información de EF a DTOs para no exponer entidades.
        return new HomeDashboardDto
        {
            NombreUsuario = usuario,
            CantidadViajes = cantidadViajes,
            CantidadEnvios = cantidadEnvios,
            CantidadOportunidades = cantidadOportunidades,
            PotencialGanancias = gananciaEfectiva + potencialOportunidades,
            GananciaEfectivaMes = gananciaEfectiva,
            Viajes = viajes.Select(v => new HomeViajeDto
            {
                Id = v.Id,
                Origen = v.OrigenDir,
                Destino = v.DestinoDir,
                HoraSalida = v.HoraSalida,
                HoraLlegada = v.HoraLlegada,
                Activo = v.Activo,
                DiasSemana = v.ViajeDias
                    .OrderBy(d => d.DiaSemana)
                    .Select(d => ObtenerNombreDia(d.DiaSemana))
                    .ToList()
            }).ToList(),
            Envios = envios.Select(e => new HomeEnvioDto
            {
                Id = e.Id,
                Origen = e.OrigenDir,
                Destino = e.DestinoDir,
                Descripcion = e.Descripcion ?? string.Empty,
                FechaLimite = e.FechaLimite,
                Monto = e.Monto,
                Estado = e.Estado,
                EstadoNombre = ObtenerNombreEstado(e.Estado)
            }).ToList(),
            Oportunidades = oportunidades.Select(e => new HomeOportunidadDto
            {
                Id = e.Id,
                Origen = e.OrigenDir,
                Destino = e.DestinoDir,
                Descripcion = e.Descripcion ?? string.Empty,
                FechaLimite = e.FechaLimite,
                Monto = e.Monto,
                PesoKg = e.PesoKg
            }).ToList()
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

    private static string ObtenerNombreEstado(byte estado) => (EstadoEnvio)estado switch
    {
        EstadoEnvio.Pendiente => "Pendiente",
        EstadoEnvio.BuscandoConductor => "Buscando conductor",
        EstadoEnvio.ConductorAsignado => "Conductor asignado",
        EstadoEnvio.Retirado => "Retirado",
        EstadoEnvio.Entregado => "Entregado",
        EstadoEnvio.Cancelado => "Cancelado",
        _ => "Estado desconocido"
    };
}
