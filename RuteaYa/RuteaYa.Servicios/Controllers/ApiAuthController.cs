using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers
{
    [Route("Api/Auth")]
    public class ApiAuthController : Controller
    {
        private UsuarioService _usuarioService;

        public ApiAuthController()
        {
            _usuarioService = new UsuarioService();
        }

        // Login: Api/Auth/Login
        [HttpPost("Login")]
        public IActionResult Login([FromBody] Usuario usuario)
        {
            if (usuario == null || string.IsNullOrEmpty(usuario.Nombre) || string.IsNullOrEmpty(usuario.PasswordHash))
            {
                return BadRequest("Nombre de usuario y contraseña son requeridos.");
            }
            // Lógica para autenticar al usuario
            bool autenticado = _usuarioService.AutenticarUsuario(usuario.Nombre, usuario.PasswordHash);
            if (autenticado)
            {
                return Ok("Autenticación exitosa.");
            }
            else
            {
                return Unauthorized("Nombre de usuario o contraseña incorrectos.");
            }
        }

        // Register: Api/Auth/Register
        [HttpPost("Register")]
        public IActionResult Register([FromBody] Usuario usuario)
        {
            if (usuario == null || string.IsNullOrEmpty(usuario.Nombre) || string.IsNullOrEmpty(usuario.PasswordHash))
            {
                return BadRequest("Nombre de usuario y contraseña son requeridos.");
            }
            // Lógica para registrar al usuario
            bool registrado = _usuarioService.RegistrarUsuario(usuario);
            if (registrado)
            {
                return Ok("Registro exitoso.");
            }
            else
            {
                return Conflict("El nombre de usuario ya está en uso.");
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
