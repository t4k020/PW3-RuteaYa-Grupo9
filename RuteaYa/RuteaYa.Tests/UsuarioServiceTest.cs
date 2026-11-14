using Microsoft.EntityFrameworkCore;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios;
using Xunit;

namespace RuteaYa.Tests;

public class UsuarioServiceTest
{
    [Fact]
    public async Task CrearUsuario_DeberiaGuardarElUsuarioConLaContrasenaHasheada()
    {
        using var context = CrearContexto();
        var service = new UsuarioService(new UsuarioRepository(context));
        var request = new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com",
            Password = "contrasena123"
        };

        var resultado = await service.CrearUsuario(request);

        Assert.True(resultado);

        var usuario = Assert.Single(await context.Usuarios.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Equal("Ana", usuario.Nombre);
        Assert.Equal("Pérez", usuario.Apellido);
        Assert.Equal("ana@test.com", usuario.Email);
        Assert.NotEqual("contrasena123", usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("contrasena123", usuario.PasswordHash));
    }

    [Fact]
    public async Task CrearUsuario_DeberiaRetornarFalseYNoCrearNada_CuandoElEmailYaEstaRegistrado()
    {
        using var context = CrearContexto();
        context.Usuarios.Add(new Usuario
        {
            Id = 1,
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("contrasena123")
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new UsuarioService(new UsuarioRepository(context));
        var request = new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@test.com",
            Password = "otraContrasena456"
        };

        var resultado = await service.CrearUsuario(request);

        Assert.False(resultado);
        Assert.Equal(1, await context.Usuarios.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListarUsuariosAsync_DeberiaMapearLosUsuariosADtos()
    {
        using var context = CrearContexto();
        var creadoEn = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        context.Usuarios.Add(new Usuario
        {
            Id = 1,
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com",
            PasswordHash = "hash",
            CreadoEn = creadoEn
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new UsuarioService(new UsuarioRepository(context));

        var resultado = await service.ListarUsuariosAsync();

        var usuario = Assert.Single(resultado);
        Assert.Equal(1, usuario.Id);
        Assert.Equal("Ana", usuario.Nombre);
        Assert.Equal("Pérez", usuario.Apellido);
        Assert.Equal("ana@test.com", usuario.Email);
        Assert.Equal(creadoEn, usuario.CreadoEn);
    }

    private static RuteaYaDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RuteaYaDbContext(options);
    }
}
