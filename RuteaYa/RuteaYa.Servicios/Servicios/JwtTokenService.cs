using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RuteaYa.Entidades;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RuteaYa.Servicios.Servicios
{
    public interface ITokenService
    {
        string GenerarToken(Usuario usuario);
    }

    /// <summary>
    /// Genera los JWT que la API entrega en el login. Toda la configuración
    /// (clave, emisor, audiencia y expiración) sale de la sección "Jwt".
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private const int MinutosExpiracionPorDefecto = 60;

        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(Usuario usuario)
        {
            var clave = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Falta configurar Jwt:Key");

            var minutosExpiracion = int.TryParse(
                _configuration["Jwt:ExpirationMinutes"],
                out var minutos)
                ? minutos
                : MinutosExpiracionPorDefecto;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email)
            };

            var credenciales = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutosExpiracion),
                signingCredentials: credenciales);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
