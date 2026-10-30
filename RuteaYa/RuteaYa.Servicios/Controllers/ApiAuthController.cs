using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers
{
    public interface IAuthService
    {
        Task<Usuario> AutenticarUsuario(string email, string passwordHash);
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

            Usuario usuarioAutenticado = await _authService.AutenticarUsuario(usuario.Email, usuario.PasswordHash);

            if (usuarioAutenticado != null)
            {
                return Ok(new
                {
                    id = usuarioAutenticado.Id,
                    nombre = usuarioAutenticado.Nombre,
                    email = usuarioAutenticado.Email,
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
            if (usuario == null)
            {
                return BadRequest("Los datos del usuario son obligatorios.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                bool registrado = await _usuarioService.CrearUsuario(usuario);

                if (!registrado)
                {
                    return Conflict(new
                    {
                        registrado = false,
                        mensaje = "El correo electrónico ya está en uso."
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
                    registrado = false,
                    mensaje = "Ocurrió un error al registrar el usuario."
                });
            }
        }
    }

    
}
