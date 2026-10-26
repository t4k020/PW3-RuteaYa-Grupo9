using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;

namespace RuteaYa.Servicios.Controllers
{
    public class ApiUsuarioController : Controller
    {
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
    }
}
