using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;
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
            if (!TryObtenerUsuarioId(out int usuarioId))
                return Unauthorized();

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
            if (!TryObtenerUsuarioId(out int usuarioId))
                return Unauthorized();

            try
            {
                var viaje = await _viajeService.ObtenerDetallePorIdAsync(id, usuarioId);

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
        [ProducesResponseType(typeof(ViajeDetalleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Crear([FromBody] CrearViajeDto solicitud)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                var detalles = new ValidationProblemDetails(ModelState)
                {
                    Title = "Datos inválidos.",
                    Status = StatusCodes.Status400BadRequest
                };
                return BadRequest(detalles);
            }

            try
            {
                var viaje = Mapear(solicitud, usuarioId);
                var viajeCreado = await _viajeService.CrearViajeAsync(viaje);
                var detalle = await _viajeService.ObtenerDetallePorIdAsync(viajeCreado.Id, usuarioId);

                return CreatedAtAction(nameof(ObtenerPorId), new { id = viajeCreado.Id }, detalle);
            }
            catch (Exception)
            {
                return StatusCode(500, "Ocurrió un error interno al crear el viaje.");
            }
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] Viaje viaje)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var actualizado = await _viajeService.ActualizarViajeAsync(id, viaje, usuarioId);

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
            if (!TryObtenerUsuarioId(out int usuarioId))
                return Unauthorized();

            try
            {
                var eliminado = await _viajeService.EliminarViajeAsync(id, usuarioId);

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
            if (!TryObtenerUsuarioId(out int usuarioId))
                return Unauthorized();

            try
            {
                var desactivado = await _viajeService.DesactivarViajeAsync(id, usuarioId);

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

        // Construye la entidad a partir del DTO. El usuario, las coordenadas y
        // la distancia/duración los define el servidor, nunca el cliente.
        private static Viaje Mapear(CrearViajeDto solicitud, int usuarioId)
        {
            var paradas = solicitud.Paradas ?? new List<ParadaViajeDto>();
            var dias = solicitud.DiasSemana ?? new List<byte>();

            return new Viaje
            {
                UsuarioId = usuarioId,
                OrigenDir = solicitud.OrigenDir.Trim(),
                OrigenLat = 0,
                OrigenLng = 0,
                DestinoDir = solicitud.DestinoDir.Trim(),
                DestinoLat = 0,
                DestinoLng = 0,
                HoraSalida = solicitud.HoraSalida!.Value,
                HoraLlegada = solicitud.HoraLlegada!.Value,
                MaxPaquetes = solicitud.MaxPaquetes,
                PesoMaxKg = solicitud.PesoMaxKg,
                DesvioMaxKm = solicitud.DesvioMaxKm,
                DistanciaKm = 0,
                DuracionMin = 0,
                Activo = true,
                ViajeDias = dias
                    .Distinct()
                    .OrderBy(dia => dia)
                    .Select(dia => new ViajeDia { DiaSemana = dia })
                    .ToList(),
                Paradas = paradas
                    .Select((parada, indice) => new Parada
                    {
                        Orden = indice + 1,
                        Direccion = parada.Direccion.Trim(),
                        Lat = 0,
                        Lng = 0
                    })
                    .ToList()
            };
        }

        // Obtiene el ID del usuario autenticado desde el JWT
        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out usuarioId
            );
        }
    }
}
