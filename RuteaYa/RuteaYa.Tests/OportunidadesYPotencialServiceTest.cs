using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios;
using RuteaYa.Servicios.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public class OportunidadesYPotencialServiceTest
{
    [Fact]
    public async Task ObtenerOportunidadesParaViajeAsync_FiltraPorFechaCapacidadPesoYDesvio()
    {
        await using var context = CrearContexto();
        var fecha = ProximaFechaProgramada();
        context.Viajes.Add(CrearViaje(1, 10, 2, 5m, 2m, fecha));
        context.Envios.AddRange(
            CrearEnvio(1, 20, fecha, 2m, 4000m),
            CrearEnvio(3, 20, fecha, 6m, 9000m), // supera peso
            CrearEnvio(5, 20, fecha, 1m, 8000m), // supera desvio estimado: 3 km
            CrearEnvio(8, 20, fecha.AddDays(-1), 1m, 7000m)); // vencido
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await new EnvioService(context, RutasDePrueba.CrearMock()).ObtenerOportunidadesParaViajeAsync(1, fecha, 10, "ganancia", TestContext.Current.CancellationToken);

        var oportunidad = Assert.Single(resultado!.Oportunidades);
        Assert.Equal(1, oportunidad.Id);
        Assert.Equal(1m, oportunidad.DesvioKm);
    }

    [Fact]
    public async Task CalcularAsync_NoReutilizaElMismoEnvioEnDosViajesDelMes()
    {
        await using var context = CrearContexto();
        var inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        var fechaUno = inicio.AddDays(1);
        var fechaDos = inicio.AddDays(2);
        context.Viajes.AddRange(
            CrearViaje(1, 10, 2, 10m, 5m, fechaUno),
            CrearViaje(2, 10, 2, 10m, 5m, fechaDos));
        context.Envios.Add(CrearEnvio(1, 20, inicio.AddDays(20), 2m, 4000m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultado = await new PotencialService(context).CalcularAsync(10, inicio.Year, inicio.Month, "ganancia", TestContext.Current.CancellationToken);

        Assert.Equal(4000m, resultado.GananciaPotencial);
        Assert.Equal(1, resultado.PaquetesPosibles);
        Assert.Single(resultado.Viajes);
    }

    [Fact]
    public async Task ObtenerOportunidadesParaViajeAsync_OrdenaPorCompatibilidad_ValorPorKmDeDesvio()
    {
        await using var context = CrearContexto();
        var fecha = ProximaFechaProgramada();
        context.Viajes.Add(CrearViaje(1, 10, 5, 50m, 10m, fecha));
        context.Envios.AddRange(
            CrearEnvio(1, 20, fecha, 1m, 4000m),
            CrearEnvio(2, 20, fecha, 1m, 5000m),
            CrearEnvio(3, 20, fecha, 1m, 9000m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var servicio = new EnvioService(context, RutasDePrueba.CrearMock());

        var porCompatibilidad = await servicio.ObtenerOportunidadesParaViajeAsync(1, fecha, 10, "compatibilidad", TestContext.Current.CancellationToken);
        var porDesvio = await servicio.ObtenerOportunidadesParaViajeAsync(1, fecha, 10, "desvio", TestContext.Current.CancellationToken);

        Assert.Equal(new[] { 3, 1, 2 }, porCompatibilidad!.Oportunidades.Select(o => o.Id));
        Assert.Equal(new[] { 1, 2, 3 }, porDesvio!.Oportunidades.Select(o => o.Id));
    }

    [Fact]
    public async Task ObtenerDetalleOportunidadParaViajeAsync_DevuelveValoresRealesDelViaje()
    {
        await using var context = CrearContexto();
        var fecha = ProximaFechaProgramada();
        var viaje = CrearViaje(1, 10, 2, 5m, 2m, fecha);
        viaje.DistanciaKm = 18.4m;
        viaje.DuracionMin = 55;
        context.Viajes.Add(viaje);
        context.Envios.Add(CrearEnvio(3, 20, fecha, 2m, 6000m)); // desvío estimado 2.0 km
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var detalle = await new EnvioService(context, RutasDePrueba.CrearMock())
            .ObtenerDetalleOportunidadParaViajeAsync(3, 1, fecha, 10, TestContext.Current.CancellationToken);

        Assert.NotNull(detalle);
        Assert.Equal(18.4m, detalle!.DistanciaHabitualKm);
        Assert.Equal(2m, detalle.DesvioKm);
        Assert.Equal(20.4m, detalle.DistanciaConEnvioKm);
        Assert.Equal(55, detalle.DuracionHabitualMin);
        Assert.Equal(6, detalle.TiempoAdicionalMin);
        Assert.Equal(2, detalle.CupoRestante);
        Assert.Equal(5m, detalle.PesoRestanteKg);
        Assert.Equal(3000m, detalle.PagoPorKmAdicional);
        Assert.Equal("Compatible", detalle.Compatibilidad);
        Assert.True(detalle.PuedeAceptar);
    }

    [Fact]
    public async Task ObtenerDetalleOportunidadParaViajeAsync_NoPermiteAceptar_CuandoElPesoNoEntra()
    {
        await using var context = CrearContexto();
        var fecha = ProximaFechaProgramada();
        context.Viajes.Add(CrearViaje(1, 10, 2, 5m, 2m, fecha));
        context.Envios.Add(CrearEnvio(1, 20, fecha, 9m, 4000m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var detalle = await new EnvioService(context, RutasDePrueba.CrearMock())
            .ObtenerDetalleOportunidadParaViajeAsync(1, 1, fecha, 10, TestContext.Current.CancellationToken);

        Assert.False(detalle!.PuedeAceptar);
    }

    [Fact]
    public async Task ObtenerDetalleOportunidadParaViajeAsync_DevuelveNull_CuandoElViajeNoEsDelUsuario()
    {
        await using var context = CrearContexto();
        var fecha = ProximaFechaProgramada();
        context.Viajes.Add(CrearViaje(1, 10, 2, 5m, 2m, fecha));
        context.Envios.Add(CrearEnvio(1, 20, fecha, 1m, 4000m));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var detalle = await new EnvioService(context, RutasDePrueba.CrearMock())
            .ObtenerDetalleOportunidadParaViajeAsync(1, 1, fecha, 99, TestContext.Current.CancellationToken);

        Assert.Null(detalle);
    }

    private static RuteaYaDbContext CrearContexto() => new(new DbContextOptionsBuilder<RuteaYaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DateTime ProximaFechaProgramada()
    {
        var fecha = DateTime.Today.AddDays(1);
        while (fecha.DayOfWeek != DayOfWeek.Monday) fecha = fecha.AddDays(1);
        return fecha;
    }

    private static Viaje CrearViaje(int id, int usuarioId, int cupo, decimal peso, decimal desvio, DateTime fecha) => new()
    {
        Id = id, UsuarioId = usuarioId, OrigenDir = "Origen", DestinoDir = "Destino", HoraSalida = new TimeOnly(8, 0), HoraLlegada = new TimeOnly(9, 0), MaxPaquetes = cupo, PesoMaxKg = peso, DesvioMaxKm = desvio, Activo = true,
        ViajeDias = new List<ViajeDia> { new() { DiaSemana = fecha.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fecha.DayOfWeek } }
    };

    private static Envio CrearEnvio(int id, int usuarioId, DateTime fechaLimite, decimal peso, decimal monto) => new()
    {
        Id = id, UsuarioId = usuarioId, OrigenDir = "Origen paquete", DestinoDir = "Destino paquete", FechaLimite = fechaLimite, PesoKg = peso, Tamano = 1, Descripcion = "Paquete", Monto = monto, Estado = (byte)EstadoEnvio.BuscandoConductor
    };
}
