using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers
{
    public interface IAuthService
    {
        Task<Usuario?> AutenticarUsuario(string email, string password);
    }

    [ApiController]
    [Route("Api/Auth")]
    [Produces("application/json")]
    public class ApiAuthController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public ApiAuthController(
            UsuarioService usuarioService,
            IAuthService authService,
            ITokenService tokenService)
        {
            _usuarioService = usuarioService;
            _authService = authService;
            _tokenService = tokenService;
        }

        /// <summary>Inicia sesión y devuelve un token JWT.</summary>
        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _authService.AutenticarUsuario(
                request.Email,
                request.PasswordHash
            );

            // Mismo mensaje para email inexistente y contraseña incorrecta:
            // no se revela cuál de los dos datos falló.
            if (usuario == null)
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Credenciales inválidas.",
                    Detail = "Email o contraseña incorrectos."
                });

            return Ok(new LoginResponse
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Token = _tokenService.GenerarToken(usuario),
                Mensaje = "Inicio de sesión exitoso."
            });
        }

        /// <summary>Registra un usuario nuevo.</summary>
        [AllowAnonymous]
        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Sin try/catch genérico: un fallo inesperado no se tapa con un mensaje fijo,
            // sube hasta el framework, que responde 500.
            bool registrado = await _usuarioService.CrearUsuario(request);

            if (!registrado)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Email duplicado.",
                    Detail = "El email ya está registrado."
                });
            }

            // La API no expone todavía un GET de usuario por id, por eso el
            // Location apunta al login, que es el siguiente paso natural.
            return CreatedAtAction(nameof(Login), new
            {
                registrado = true,
                mensaje = "Registro exitoso."
            });
        }
    }
}
