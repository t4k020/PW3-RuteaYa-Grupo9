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
    public async Task CrearUsuario_DeberiaRetornarFalse_CuandoLaBaseRechazaUnEmailDuplicadoConcurrente()
    {
        // Otro registro con el mismo email gana la carrera entre la consulta y el INSERT.
        using var context = new ContextoQueFallaAlGuardar(CrearOpciones(), emailConcurrente: "ana@test.com");
        var service = new UsuarioService(new UsuarioRepository(context));

        var resultado = await service.CrearUsuario(new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@test.com",
            Password = "contrasena123"
        });

        Assert.False(resultado);
    }

    [Fact]
    public async Task CrearUsuario_DeberiaPropagarLaExcepcion_CuandoFallaElGuardadoPorOtroMotivo()
    {
        using var context = new ContextoQueFallaAlGuardar(CrearOpciones(), emailConcurrente: null);
        var service = new UsuarioService(new UsuarioRepository(context));

        await Assert.ThrowsAsync<DbUpdateException>(() => service.CrearUsuario(new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@test.com",
            Password = "contrasena123"
        }));
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

    private static DbContextOptions<RuteaYaDbContext> CrearOpciones()
    {
        return new DbContextOptionsBuilder<RuteaYaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static RuteaYaDbContext CrearContexto()
    {
        return new RuteaYaDbContext(CrearOpciones());
    }

    // Simula el índice único de SQL Server, que el proveedor InMemory no aplica:
    // si hay un email "concurrente" lo inserta (como si otro request lo hubiera
    // guardado primero) y luego falla el SaveChanges con DbUpdateException.
    private sealed class ContextoQueFallaAlGuardar : RuteaYaDbContext
    {
        private readonly string? _emailConcurrente;

        public ContextoQueFallaAlGuardar(
            DbContextOptions<RuteaYaDbContext> options,
            string? emailConcurrente) : base(options)
        {
            _emailConcurrente = emailConcurrente;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ChangeTracker.Clear();

            if (_emailConcurrente != null)
            {
                Usuarios.Add(new Usuario
                {
                    Nombre = "Otro",
                    Apellido = "Request",
                    Email = _emailConcurrente,
                    PasswordHash = "hash"
                });

                await base.SaveChangesAsync(cancellationToken);
            }

            throw new DbUpdateException("Violación del índice único IX_Usuarios_Email.");
        }
    }
}
