using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public class HomeServiceTest
{
    [Fact]
    public async Task ObtenerDashboardAsync_DeberiaCalcularConteosGananciasYMapearListas()
    {
        var fechaActual = new DateTime(2026, 11, 4, 12, 0, 0);
        var options = CrearOpcionesMemoria();

        await using (var context = new RuteaYaDbContext(options))
        {
            var viaje = CrearViaje(1, 8, true);
            viaje.ViajeDias.Add(new ViajeDia { ViajeId = 1, DiaSemana = 3 });
            viaje.ViajeDias.Add(new ViajeDia { ViajeId = 1, DiaSemana = 1 });

            context.Usuarios.AddRange(
                new Usuario { Id = 1, Nombre = "Ana", Apellido = "Pérez" },
                new Usuario { Id = 2, Nombre = "Luis", Apellido = "Gómez" });
            context.Viajes.Add(viaje);
            context.Viajes.Add(CrearViaje(2, 9, false, 2));

            context.Envios.AddRange(
                CrearEnvio(1, 1, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(2), 1000m, "Pendiente propio"),
                CrearEnvio(2, 1, EstadoEnvio.Entregado, fechaActual.AddDays(1), 2000m, "Entregado"),
                CrearEnvio(3, 1, EstadoEnvio.Cancelado, fechaActual.AddDays(3), 3000m, "Cancelado"),
                CrearEnvio(4, 2, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(1), 2500m, "Oportunidad"),
                CrearEnvio(5, 2, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(-1), 4000m, "Vencido"),
                CrearEnvio(6, 2, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(4), 5000m, "Asignado"));

            context.Asignaciones.AddRange(
                new Asignacion
                {
                    Id = 1,
                    EnvioId = 6,
                    ViajeId = 2,
                    FechaViaje = fechaActual.AddDays(2),
                    Ganancia = 5000m
                },
                new Asignacion
                {
                    Id = 2,
                    EnvioId = 2,
                    ViajeId = 1,
                    FechaViaje = fechaActual.AddDays(-2),
                    Ganancia = 1200m
                });

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var contextoConsulta = new RuteaYaDbContext(options);
        var service = new HomeService(contextoConsulta);

        var resultado = await service.ObtenerDashboardAsync(
            1,
            fechaActual,
            TestContext.Current.CancellationToken);

        Assert.Equal("Ana", resultado.NombreUsuario);
        Assert.Equal(1, resultado.CantidadViajes);
        Assert.Equal(1, resultado.CantidadEnvios);
        Assert.Equal(1, resultado.CantidadOportunidades);
        Assert.Equal(1200m, resultado.GananciaEfectivaMes);
        Assert.Equal(7700m, resultado.PotencialGanancias);

        var viajeDto = Assert.Single(resultado.Viajes);
        Assert.Equal(new[] { "Lun", "Mié" }, viajeDto.DiasSemana);

        var envioDto = Assert.Single(resultado.Envios);
        Assert.Equal(1, envioDto.Id);
        Assert.Equal("Pendiente propio", envioDto.Descripcion);
        Assert.Equal("Buscando conductor", envioDto.EstadoNombre);

        var oportunidadDto = Assert.Single(resultado.Oportunidades);
        Assert.Equal(4, oportunidadDto.Id);
        Assert.Equal("Oportunidad", oportunidadDto.Descripcion);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_DeberiaExcluirOportunidadesAsignadasYFueraDeFecha()
    {
        var fechaActual = new DateTime(2026, 11, 4);
        var options = CrearOpcionesMemoria();

        await using (var context = new RuteaYaDbContext(options))
        {
            context.Usuarios.AddRange(
                new Usuario { Id = 1, Nombre = "Ana" },
                new Usuario { Id = 2, Nombre = "Luis" });
            context.Viajes.Add(CrearViaje(10, 7, true));
            context.Envios.AddRange(
                CrearEnvio(1, 2, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(1), 100m, "Disponible"),
                CrearEnvio(2, 2, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(-1), 200m, "Vencido"),
                CrearEnvio(3, 2, EstadoEnvio.BuscandoConductor, fechaActual.AddDays(2), 300m, "Tomado"));
            context.Asignaciones.Add(new Asignacion
            {
                EnvioId = 3,
                ViajeId = 10,
                FechaViaje = fechaActual,
                Ganancia = 300m
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var contextoConsulta = new RuteaYaDbContext(options);
        var resultado = await new HomeService(contextoConsulta)
            .ObtenerDashboardAsync(1, fechaActual, TestContext.Current.CancellationToken);

        Assert.Equal(1, resultado.CantidadOportunidades);
        var oportunidad = Assert.Single(resultado.Oportunidades);
        Assert.Equal(1, oportunidad.Id);
        Assert.Equal(600m, resultado.PotencialGanancias);
    }

    [Fact]
    public async Task ObtenerDashboardAsync_DeberiaLanzarKeyNotFoundException_CuandoElUsuarioNoExiste()
    {
        await using var context = new RuteaYaDbContext(CrearOpcionesMemoria());
        var service = new HomeService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ObtenerDashboardAsync(
                999,
                new DateTime(2026, 11, 4),
                TestContext.Current.CancellationToken));
    }

    private static DbContextOptions<RuteaYaDbContext> CrearOpcionesMemoria()
    {
        return new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static Viaje CrearViaje(int id, int horaSalida, bool activo, int usuarioId = 1)
    {
        return new Viaje
        {
            Id = id,
            UsuarioId = usuarioId,
            OrigenDir = $"Origen {id}",
            DestinoDir = $"Destino {id}",
            HoraSalida = new TimeOnly(horaSalida, 0),
            HoraLlegada = new TimeOnly(horaSalida + 1, 0),
            MaxPaquetes = 3,
            PesoMaxKg = 10m,
            Activo = activo
        };
    }

    private static Envio CrearEnvio(
        int id,
        int usuarioId,
        EstadoEnvio estado,
        DateTime fechaLimite,
        decimal monto,
        string descripcion)
    {
        return new Envio
        {
            Id = id,
            UsuarioId = usuarioId,
            OrigenDir = $"Origen envío {id}",
            DestinoDir = $"Destino envío {id}",
            FechaLimite = fechaLimite,
            PesoKg = 1m,
            Tamano = 1,
            Monto = monto,
            Estado = (byte)estado,
            Descripcion = descripcion
        };
    }
}
