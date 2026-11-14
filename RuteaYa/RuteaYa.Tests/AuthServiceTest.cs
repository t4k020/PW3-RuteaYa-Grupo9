using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public class AuthServiceTest
{
    [Fact]
    public async Task AutenticarUsuario_DeberiaRetornarElUsuario_CuandoLasCredencialesSonValidas()
    {
        using var context = CrearContexto();
        context.Usuarios.Add(CrearUsuario(1, "ana@test.com", "contrasena123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new AuthService(new UsuarioRepository(context));

        var resultado = await service.AutenticarUsuario("ana@test.com", "contrasena123");

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado!.Id);
        Assert.Equal("ana@test.com", resultado.Email);
    }

    [Fact]
    public async Task AutenticarUsuario_DeberiaRetornarNull_CuandoElEmailNoExiste()
    {
        using var context = CrearContexto();
        context.Usuarios.Add(CrearUsuario(1, "ana@test.com", "contrasena123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new AuthService(new UsuarioRepository(context));

        var resultado = await service.AutenticarUsuario("otro@test.com", "contrasena123");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task AutenticarUsuario_DeberiaRetornarNull_CuandoLaContrasenaEsIncorrecta()
    {
        using var context = CrearContexto();
        context.Usuarios.Add(CrearUsuario(1, "ana@test.com", "contrasena123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new AuthService(new UsuarioRepository(context));

        var resultado = await service.AutenticarUsuario("ana@test.com", "contrasena-equivocada");

        Assert.Null(resultado);
    }

    private static RuteaYaDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RuteaYaDbContext(options);
    }

    private static Usuario CrearUsuario(int id, string email, string password)
    {
        return new Usuario
        {
            Id = id,
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
    }
}
