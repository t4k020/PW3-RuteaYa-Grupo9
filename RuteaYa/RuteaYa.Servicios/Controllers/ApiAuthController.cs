
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RuteaYa.Servicios.Controllers
{
    public interface IAuthService
    {
        Task<Usuario?> AutenticarUsuario(string email, string password);
    }

    [ApiController]
    [Route("Api/Auth")]
    public class ApiAuthController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public ApiAuthController(
            UsuarioService usuarioService,
            IAuthService authService,
            IConfiguration configuration)
        {
            _usuarioService = usuarioService;
            _authService = authService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _authService.AutenticarUsuario(
                request.Email,
                request.PasswordHash
            );

            if (usuario == null)
                return Unauthorized(new
                {
                    mensaje = "Email o contraseña incorrectos."
                });

            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var expirationMinutes = int.Parse(
                _configuration["Jwt:ExpirationMinutes"] ?? "60"
            );

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponse
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Token = tokenString,
                Mensaje = "Inicio de sesión exitoso."
            });
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                bool registrado =
                    await _usuarioService.CrearUsuario(request);

                if (!registrado)
                {
                    return Conflict(new
                    {
                        mensaje = "El email ya está registrado."
                    });
                }

                return Ok(new
                {
                    registrado = true,
                    mensaje = "Registro exitoso."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error al registrar el usuario."
                });
            }
        }
    }
}
