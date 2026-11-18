using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using Xunit;

namespace RuteaYa.Tests;

public class DbInitializerTest
{
    private const string OrigenViejo = "Calle Falsa 123, Morón";
    private const string OrigenNuevo = "Av. Rivadavia 18500, Morón";
    private const string DestinoDemo = "Plaza de Mayo, CABA";
    private const string ParadaVieja = "Gaona 2500, Morón";
    private const string ParadaNueva = "Machado 854, Morón";
    private const string DescripcionVieja = "Envío cancelado de prueba";
    private const string DescripcionNueva = "Caja de vajilla";

    [Fact]
    public void Initialize_CorrigeLosDatosDemoDeUnaBaseExistente()
    {
        using var context = CrearContextoConDatosDemoViejos();

        var viajeId = context.Viajes.Single(v => v.OrigenDir == OrigenViejo).Id;
        var envioId = context.Envios.Single(e => e.Descripcion == DescripcionVieja).Id;

        DbInitializer.Initialize(context);

        var viaje = context.Viajes.Single(v => v.Id == viajeId);
        Assert.Equal(OrigenNuevo, viaje.OrigenDir);
        Assert.Equal(-34.6527m, viaje.OrigenLat);
        Assert.Equal(-58.6198m, viaje.OrigenLng);
        Assert.DoesNotContain(context.Viajes, v => v.OrigenDir == OrigenViejo);

        var parada = context.Paradas.Single(p => p.ViajeId == viajeId);
        Assert.Equal(ParadaNueva, parada.Direccion);
        Assert.Equal(-34.6557m, parada.Lat);
        Assert.Equal(-58.6210m, parada.Lng);
        Assert.DoesNotContain(context.Paradas, p => p.Direccion == ParadaVieja);

        var envio = context.Envios.Single(e => e.Id == envioId);
        Assert.Equal(DescripcionNueva, envio.Descripcion);
        Assert.DoesNotContain(context.Envios, e => e.Descripcion == DescripcionVieja);
    }

    [Fact]
    public void Initialize_NoDuplicaDatosCuandoYaEstanNormalizados()
    {
        using var context = CrearContextoConDatosDemoViejos();

        DbInitializer.Initialize(context);

        var viajes = context.Viajes.Count();
        var paradas = context.Paradas.Count();
        var envios = context.Envios.Count();

        DbInitializer.Initialize(context);

        Assert.Equal(viajes, context.Viajes.Count());
        Assert.Equal(paradas, context.Paradas.Count());
        Assert.Equal(envios, context.Envios.Count());
        Assert.Single(context.Viajes.Where(v => v.OrigenDir == OrigenNuevo && v.DestinoDir == DestinoDemo));
        Assert.Single(context.Paradas.Where(p => p.Direccion == ParadaNueva));
        Assert.Single(context.Envios.Where(e => e.Descripcion == DescripcionNueva));
    }

    private static RuteaYaDbContext CrearContextoConDatosDemoViejos()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RuteaYaDbContext(options);

        var admin = new Usuario
        {
            Nombre = "Admin",
            Apellido = "Sistema",
            Email = "admin@ruteaya.com",
            PasswordHash = "hash",
            CreadoEn = DateTime.Now
        };

        var carlos = new Usuario
        {
            Nombre = "Carlos",
            Apellido = "González",
            Email = "carlos.gonzalez@example.com",
            PasswordHash = "hash",
            CreadoEn = DateTime.Now
        };

        var lucia = new Usuario
        {
            Nombre = "Lucía",
            Apellido = "Martínez",
            Email = "lucia.martinez@example.com",
            PasswordHash = "hash",
            CreadoEn = DateTime.Now
        };

        context.Usuarios.AddRange(admin, carlos, lucia);
        context.SaveChanges();

        var viaje = new Viaje
        {
            UsuarioId = carlos.Id,
            OrigenDir = OrigenViejo,
            OrigenLat = -34.6534m,
            OrigenLng = -58.6192m,
            DestinoDir = DestinoDemo,
            DestinoLat = -34.6083m,
            DestinoLng = -58.3712m,
            HoraSalida = new TimeOnly(9, 30),
            HoraLlegada = new TimeOnly(11, 0),
            MaxPaquetes = 3,
            PesoMaxKg = 30m,
            DesvioMaxKm = 3.5m,
            DistanciaKm = 22m,
            DuracionMin = 60,
            Activo = true
        };

        context.Viajes.Add(viaje);
        context.SaveChanges();

        context.Paradas.Add(new Parada
        {
            ViajeId = viaje.Id,
            Orden = 1,
            Direccion = ParadaVieja,
            Lat = -34.625m,
            Lng = -58.47m
        });

        context.Envios.Add(new Envio
        {
            UsuarioId = admin.Id,
            OrigenDir = "Av. Las Heras 1200, CABA",
            OrigenLat = -34.59m,
            OrigenLng = -58.402m,
            DestinoDir = "Av. Santa Fe 2000, CABA",
            DestinoLat = -34.595m,
            DestinoLng = -58.405m,
            FechaLimite = DateTime.Today.AddDays(2),
            PesoKg = 4m,
            Tamano = 2,
            Descripcion = DescripcionVieja,
            Monto = 2800m,
            Estado = (byte)EstadoEnvio.Cancelado,
            CreadoEn = DateTime.Now
        });

        context.SaveChanges();

        return context;
    }
}
