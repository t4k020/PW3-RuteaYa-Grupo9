using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers
{
    public interface IAuthService
    {
        Task<bool> AutenticarUsuario(string nombre, string passwordHash);
    }

    [Route("Api/Auth")]
    public class ApiAuthController : Controller
    {
        private UsuarioService _usuarioService;
        private IAuthService _authService;

        public ApiAuthController(UsuarioService usuarioService, IAuthService authService)
        {
            _usuarioService = usuarioService;
            _authService = authService;
        }

        // POST: Api/Auth/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Usuario usuario)
        {
            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Email) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
            {
                return BadRequest(new
                {
                    autenticado = false,
                    mensaje = "Correo electrónico y contraseña son requeridos."
                });
            }

            bool autenticado = await _authService.AutenticarUsuario(usuario.Email, usuario.PasswordHash);

            if (autenticado)
            {
                return Ok(new
                {
                    autenticado = true,
                    mensaje = "Autenticación exitosa."
                });
            }

            return Unauthorized(new
            {
                autenticado = false,
                mensaje = "Correo electrónico o contraseña incorrectos."
            });
        }

        // Register: Api/Auth/Register
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] Usuario usuario)
        {
            if (usuario == null || string.IsNullOrEmpty(usuario.Email) || string.IsNullOrEmpty(usuario.PasswordHash))
            {
                return BadRequest("Correo electrónico y contraseña son requeridos.");
            }
            // Lógica para registrar al usuario
            bool registrado = await _usuarioService.CrearUsuario(usuario);
            if (registrado)
            {
                return Ok("Registro exitoso.");
            }
            else
            {
                return Conflict("El correo electrónico ya está en uso.");
            }
        }

        //Logout: Api/Auth/Logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();

            return Ok(new
            {
                mensaje = "Sesión cerrada correctamente"
            });
        }
    }

    
}
