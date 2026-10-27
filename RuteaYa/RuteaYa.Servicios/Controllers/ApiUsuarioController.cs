using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers
{
    [Route("Api/Usuario")]
    public class ApiUsuarioController : Controller
    {
        private UsuarioService _usuarioService;

        public ApiUsuarioController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: Api/Usuario/id
        [HttpGet("{id}")]
        public Usuario ObtenerUsuarioPorId(int id)
        {
            Usuario usuario = new Usuario();
           
            if (id <= 0)
            {
                throw new ArgumentException("El ID del usuario debe ser mayor que cero.");
            }
            // Lógica para obtener el usuario
            return usuario;

        }

        // PUT: Api/Usuario/id
        [HttpPut("{id}")]
        public Boolean EditarUsuarioPorId(int id, [FromBody] Usuario usuario)
        {
            if (usuario == null)
            {
                return false;
            }
            // Lógica para actualizar el usuario
            return true;
        }

        // DELETE: Api/Usuario/id
        [HttpDelete("{id}")]
        public Boolean EliminarUsuarioPorId(int id)
        {
            if (id <= 0)
            {
                return false;
            }
            // Lógica para eliminar el usuario
            return true;
        }

        // Todos los usuarios: Api/Usuarios
        [HttpGet("List")]
        public async Task<IActionResult> ListarUsuarios()
        {
            var usuarios = await _usuarioService.ListarUsuariosAsync();

            return Ok(usuarios);
        }
    }
}
