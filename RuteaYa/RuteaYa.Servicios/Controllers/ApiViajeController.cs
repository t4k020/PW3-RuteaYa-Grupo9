using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;

namespace RuteaYa.Servicios.Controllers
{
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

            //if (!User?.Identity?.IsAuthenticated ?? true)
            //{
            //    return Unauthorized();
            //}

            try
            {
                // Llamamos al método del servicio que devuelve el DTO
                var viajesDto = await _viajeService.ObtenerParaListadoAsync(soloActivo);
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
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

            try
            {
                var viaje = await _viajeService.ObtenerPorIdAsync(id);

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
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
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
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }
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
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }

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
            //if (!User?.Identity?.IsAuthenticated ?? true)
            //{
            //    return Unauthorized();
            //}

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
