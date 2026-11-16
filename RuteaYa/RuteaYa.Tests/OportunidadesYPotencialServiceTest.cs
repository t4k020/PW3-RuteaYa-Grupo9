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

        var resultado = await new EnvioService(context).ObtenerOportunidadesParaViajeAsync(1, fecha, 10, "ganancia", TestContext.Current.CancellationToken);

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
