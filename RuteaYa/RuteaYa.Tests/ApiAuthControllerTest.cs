using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RuteaYa.Entidades;
using RuteaYa.Repositorios;
using RuteaYa.Servicios.Controllers;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace RuteaYa.Tests;

public class ApiAuthControllerTest
{
    private const string ClaveJwt = "clave-de-prueba-super-segura-de-32-caracteres";
    private const string EmisorJwt = "RuteaYa.Tests";
    private const string AudienciaJwt = "RuteaYa.Tests.Clientes";
    private const string MensajeCredencialesInvalidas = "Email o contraseña incorrectos.";

    [Fact]
    public async Task Login_DeberiaRetornarUnTokenValido_CuandoLasCredencialesSonValidas()
    {
        using var context = CrearContexto();
        context.Usuarios.Add(CrearUsuario(7, "ana@test.com", "contrasena123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CrearController(context);
        var request = new LoginRequest
        {
            Email = "ana@test.com",
            PasswordHash = "contrasena123"
        };

        var resultado = await controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var respuesta = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal(7, respuesta.Id);
        Assert.Equal("Ana", respuesta.Nombre);
        Assert.Equal("ana@test.com", respuesta.Email);
        Assert.False(string.IsNullOrWhiteSpace(respuesta.Token));

        var principal = ValidarToken(respuesta.Token);
        Assert.Equal("7", principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("Ana", principal.FindFirst(ClaimTypes.Name)!.Value);
        Assert.Equal("ana@test.com", principal.FindFirst(ClaimTypes.Email)!.Value);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(respuesta.Token);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(55));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(61));
    }

    [Theory]
    [InlineData("otro@test.com", "contrasena123")]
    [InlineData("ana@test.com", "contrasena-equivocada")]
    public async Task Login_DeberiaRetornarUnauthorizedConMensajeGenerico_CuandoLasCredencialesSonInvalidas(
        string email,
        string password)
    {
        using var context = CrearContexto();
        context.Usuarios.Add(CrearUsuario(7, "ana@test.com", "contrasena123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CrearController(context);

        var resultado = await controller.Login(new LoginRequest
        {
            Email = email,
            PasswordHash = password
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(resultado);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.Equal(MensajeCredencialesInvalidas, LeerMensaje(unauthorized.Value));
    }

    [Fact]
    public async Task Login_DeberiaRetornarBadRequest_CuandoElModeloEsInvalido()
    {
        using var context = CrearContexto();
        var controller = CrearController(context);
        controller.ModelState.AddModelError(
            nameof(LoginRequest.Email),
            "El email no es válido.");

        var resultado = await controller.Login(new LoginRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task Register_DeberiaCrearElUsuario_CuandoElEmailNoEstaRegistrado()
    {
        using var context = CrearContexto();
        var controller = CrearController(context);
        var request = new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com",
            Password = "contrasena123"
        };

        var resultado = await controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(200, ok.StatusCode);
        Assert.Contains("registrado", JsonSerializer.Serialize(ok.Value));

        var usuario = Assert.Single(await context.Usuarios.ToListAsync(
            TestContext.Current.CancellationToken));

        Assert.Equal("ana@test.com", usuario.Email);
        Assert.NotEqual("contrasena123", usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("contrasena123", usuario.PasswordHash));
    }

    [Fact]
    public async Task Register_DeberiaRetornarConflict_CuandoElEmailYaEstaRegistrado()
    {
        using var context = CrearContexto();
        context.Usuarios.Add(CrearUsuario(1, "ana@test.com", "contrasena123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CrearController(context);
        var request = new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Gómez",
            Email = "ana@test.com",
            Password = "otraContrasena456"
        };

        var resultado = await controller.Register(request);

        var conflict = Assert.IsType<ConflictObjectResult>(resultado);
        Assert.Equal(409, conflict.StatusCode);
        Assert.Equal("El email ya está registrado.", LeerMensaje(conflict.Value));
        Assert.Equal(1, await context.Usuarios.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_DeberiaRetornarBadRequest_CuandoElModeloEsInvalido()
    {
        using var context = CrearContexto();
        var controller = CrearController(context);
        controller.ModelState.AddModelError(
            nameof(RegisterRequest.Password),
            "La contraseña debe tener al menos 8 caracteres.");

        var resultado = await controller.Register(new RegisterRequest());

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(await context.Usuarios.ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_DeberiaRetornarError500_CuandoOcurreUnErrorInesperado()
    {
        var context = CrearContexto();
        var controller = CrearController(context);
        context.Dispose();
        var request = new RegisterRequest
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com",
            Password = "contrasena123"
        };

        var resultado = await controller.Register(request);

        var error = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(500, error.StatusCode);
        Assert.Equal("Error al registrar el usuario.", LeerMensaje(error.Value));
    }

    private static ApiAuthController CrearController(RuteaYaDbContext context)
    {
        var usuarioService = new UsuarioService(new UsuarioRepository(context));
        var authService = new AuthService(new UsuarioRepository(context));

        return new ApiAuthController(
            usuarioService,
            authService,
            CrearConfiguracion());
    }

    private static IConfiguration CrearConfiguracion()
    {
        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = ClaveJwt,
            ["Jwt:Issuer"] = EmisorJwt,
            ["Jwt:Audience"] = AudienciaJwt,
            ["Jwt:ExpirationMinutes"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

    private static ClaimsPrincipal ValidarToken(string token)
    {
        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = EmisorJwt,
            ValidateAudience = true,
            ValidAudience = AudienciaJwt,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(ClaveJwt)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        return new JwtSecurityTokenHandler().ValidateToken(
            token,
            parametros,
            out _);
    }

    private static string? LeerMensaje(object? valor)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(valor));

        return json.RootElement.TryGetProperty("mensaje", out var mensaje)
            ? mensaje.GetString()
            : null;
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
