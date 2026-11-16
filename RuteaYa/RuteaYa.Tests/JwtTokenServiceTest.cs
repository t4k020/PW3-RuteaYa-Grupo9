using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Servicios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace RuteaYa.Tests;

public class JwtTokenServiceTest
{
    private const string ClaveJwt = "clave-de-prueba-super-segura-de-32-caracteres";
    private const string EmisorJwt = "RuteaYa.Tests";
    private const string AudienciaJwt = "RuteaYa.Tests.Clientes";

    [Fact]
    public void GenerarToken_DeberiaIncluirLosClaimsDelUsuario_YSerValidoConLaClaveConfigurada()
    {
        var service = new JwtTokenService(CrearConfiguracion());

        var token = service.GenerarToken(CrearUsuario());

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = EmisorJwt,
                ValidateAudience = true,
                ValidAudience = AudienciaJwt,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClaveJwt)),
                ClockSkew = TimeSpan.FromSeconds(30)
            },
            out _);

        Assert.Equal("7", principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("Ana", principal.FindFirst(ClaimTypes.Name)!.Value);
        Assert.Equal("ana@test.com", principal.FindFirst(ClaimTypes.Email)!.Value);
    }

    [Fact]
    public void GenerarToken_DeberiaUsarLosMinutosDeExpiracionConfigurados()
    {
        var service = new JwtTokenService(CrearConfiguracion(expiracion: "120"));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerarToken(CrearUsuario()));

        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(115));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(121));
    }

    [Fact]
    public void GenerarToken_DeberiaExpirarEn60Minutos_CuandoNoHayExpiracionConfigurada()
    {
        var service = new JwtTokenService(CrearConfiguracion(expiracion: null));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerarToken(CrearUsuario()));

        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(55));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public void GenerarToken_DeberiaLanzarInvalidOperationException_CuandoFaltaLaClave()
    {
        var service = new JwtTokenService(CrearConfiguracion(clave: null));

        var ex = Assert.Throws<InvalidOperationException>(() => service.GenerarToken(CrearUsuario()));

        Assert.Contains("Jwt:Key", ex.Message);
    }

    private static IConfiguration CrearConfiguracion(
        string? clave = ClaveJwt,
        string? expiracion = "60")
    {
        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = clave,
            ["Jwt:Issuer"] = EmisorJwt,
            ["Jwt:Audience"] = AudienciaJwt,
            ["Jwt:ExpirationMinutes"] = expiracion
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();
    }

    private static Usuario CrearUsuario()
    {
        return new Usuario
        {
            Id = 7,
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = "ana@test.com"
        };
    }
}
