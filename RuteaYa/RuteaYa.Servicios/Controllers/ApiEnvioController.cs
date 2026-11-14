using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios;

namespace RuteaYa.Servicios.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ApiEnvioController : Controller
    {
        private readonly IEnvioService _envioService;

        public ApiEnvioController(IEnvioService envioService)
        {
            _envioService = envioService;
        }

        [HttpPost]
        public async Task<IActionResult> CrearEnvio([FromBody] Envio envio)
        {
            /*if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }*/

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Devuelve código 400 Bad Request
            }

            try
            {
                
                var nuevoEnvio = await _envioService.CrearEnvioAsync(envio);

                // Respuesta exitosa código 201 
                return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoEnvio.Id }, nuevoEnvio);
            }
            catch (InvalidOperationException ex)
            {
                // Código 400 
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception)
            {
                // Error interno del servidor  Código 500
                return StatusCode(500, "Ocurrió un error interno al procesar el envío.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            /*if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }*/

            try
            {
                var envio = await _envioService.ObtenerPorIdAsync(id);

                if (envio == null)
                {
                    return NotFound(); // código 404 si no se encuentra
                }

                return Ok(envio); // código 200 objeto encontrado
            }
            catch (Exception)
            {
                // Error interno Código 500
                return StatusCode(500, "Ocurrió un error interno al obtener el envío.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarEnvio(int id)
        {
            /*if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized();
            }*/

            try
            {
                var eliminado = await _envioService.EliminarEnvioAsync(id);

                if (!eliminado)
                {
                    return NotFound(); //código 404
                }

                return NoContent(); //  código 204 
            }
            catch (Exception)
            {
                //Código 500
                return StatusCode(500, "Ocurrió un error interno al eliminar el envío.");
            }
        }
    }


}
