using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using System.Security.Claims;

namespace RuteaYa.Servicios.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ApiViajeController : ControllerBase
    {
        private readonly IViajeService _viajeService;

        public ApiViajeController(IViajeService viajeService)
        {
            _viajeService = viajeService ?? throw new ArgumentNullException(nameof(viajeService));
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos([FromQuery] bool? soloActivo)
        {
            // Obtenemos el ID del usuario desde el JWT
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int usuarioId))
            {
                return Unauthorized();
            }

            try
            {
                // Llamamos al método del servicio que devuelve el DTO
                var viajesDto = await _viajeService.ObtenerParaListadoAsync(soloActivo, usuarioId);
                return Ok(viajesDto);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al obtener los viajes.");
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            /*if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }*/

            try
            {
                var viaje = await _viajeService.ObtenerDetallePorIdAsync(id);

                if (viaje == null)
                {
                    return NotFound();
                }

                return Ok(viaje);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al obtener el viaje.");
            }
        }

        
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] Viaje viaje)
        {
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int usuarioId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                viaje.UsuarioId = usuarioId;
                
                var viajeCreado = await _viajeService.CrearViajeAsync(viaje);
                return CreatedAtAction(nameof(ObtenerPorId), new { id = viajeCreado.Id }, viajeCreado);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al crear el viaje.");
            }
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] Viaje viaje)
        {
            /*if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }*/
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var actualizado = await _viajeService.ActualizarViajeAsync(id, viaje);

                if (!actualizado)
                {
                    return NotFound();
                }

                return NoContent(); // 204 
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al actualizar el viaje.");
            }
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            /*if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }*/

            try
            {
                var eliminado = await _viajeService.EliminarViajeAsync(id);

                if (!eliminado)
                {
                    return NotFound();
                }

                return NoContent(); // 204 No Content 
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al eliminar el viaje.");
            }
        }

        //desactivar un viaje
        [HttpPost("{id}/desactivar")]
        public async Task<IActionResult> Desactivar(int id)
        {
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

            try
            {
                var desactivado = await _viajeService.DesactivarViajeAsync(id);

                if (!desactivado)
                {
                    return NotFound();
                }

                return NoContent(); // 204 No Content 
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al desactivar el viaje.");
            }
        }
    }
}
