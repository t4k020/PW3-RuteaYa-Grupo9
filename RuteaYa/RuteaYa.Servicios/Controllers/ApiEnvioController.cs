using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Controllers;

/// <summary>
/// API REST para gestionar envíos. 
/// Todos los endpoints requieren autenticación (JWT).
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ApiEnvioController : ControllerBase
{
    // Inyección de dependencia del servicio de envíos
    private readonly IEnvioService _envioService;

    public ApiEnvioController(IEnvioService envioService)
    {
        _envioService = envioService;
    }

    /// <summary>
    /// GET: Obtiene todos los envíos del usuario autenticado.
    /// Permite filtrar por estado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EnvioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerMisEnvios(
        [FromQuery] EstadoEnvio? estado,
        CancellationToken cancellationToken)
    {
        // Validar autenticación: extraer usuarioId del JWT
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        // Obtener envíos del usuario desde el servicio
        var envios = await _envioService.ObtenerListadoPorUsuarioAsync(
            usuarioId,
            estado,
            cancellationToken);
        return Ok(envios);
    }

    /// <summary>
    /// POST: Crear un nuevo envío. El usuarioId se obtiene del JWT (no del cliente).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EnvioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CrearEnvio(
        [FromBody] CrearEnvioDto solicitud)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        // Validar modelo recibido
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            // CLAVE: Construir entidad en el servidor (no confiar en datos del cliente).
            // UsuarioId y Estado se asignan acá, nunca desde el cliente (over-posting).
            var envio = new Envio
            {
                OrigenDir = solicitud.OrigenDir.Trim(),
                DestinoDir = solicitud.DestinoDir.Trim(),
                FechaLimite = solicitud.FechaLimite.Date,
                PesoKg = solicitud.PesoKg,
                Tamano = solicitud.Tamano,
                Descripcion = solicitud.Descripcion.Trim(),
                Monto = solicitud.Monto
            };
            var nuevoEnvio = await _envioService.CrearEnvioAsync(envio, usuarioId);

            // Retornar 201 Created con la URL del nuevo recurso
            return CreatedAtAction(
                nameof(ObtenerPorIdApi),
                new { id = nuevoEnvio.Id },
                Mapear(nuevoEnvio));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Datos inválidos.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// GET: Obtiene un envío específico por su ID (solo si pertenece al usuario).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnvioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPorIdApi(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            // Obtener envío del usuario específico
            var envio = await _envioService.ObtenerDetalleDtoAsync(
                id,
                usuarioId,
                HttpContext.RequestAborted);
            return envio == null ? NotFound() : Ok(envio);
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al obtener el envío.");
        }
    }

    /// <summary>
    /// DELETE: Elimina un envío (solo si pertenece al usuario).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EliminarEnvioApi(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            // Eliminar envío del usuario
            var eliminado = await _envioService.EliminarEnvioAsync(id, usuarioId);
            return eliminado ? NoContent() : NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al eliminar el envío.");
        }
    }

    /// <summary>
    /// POST: Cancela un envío existente (solo si pertenece al usuario y está en estado válido).
    /// </summary>
    [HttpPost("{id:int}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelarEnvioApi(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            // Cancelar envío (valida estado antes de cancelar)
            var cancelado = await _envioService.CancelarEnvioAsync(id, usuarioId);
            return cancelado ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            // Error: estado no válido para cancelación
            return BadRequest(new ProblemDetails
            {
                Title = "No se puede cancelar el envío.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al cancelar el envío.");
        }
    }

    [HttpPost("{id:int}/asignar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VincularEnvioApi(int id, [FromBody] AsignarEnvioDto solicitud)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            // Ejecuta la lógica de vinculación en el servicio
            await _envioService.VincularEnvioAViajeAsync(
                id,
                solicitud.ViajeId,
                solicitud.FechaViaje,
                usuarioId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // Maneja los errores de reglas de negocio exactamente igual que en CancelarEnvioApi
            return BadRequest(new ProblemDetails
            {
                Title = "No se puede asignar el envío.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al intentar vincular el envío.");
        }
    }

    [HttpGet("oportunidades")]
    [ProducesResponseType(typeof(IEnumerable<EnvioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerOportunidadesApi(CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {

            var envios = await _envioService.ObtenerOportunidadesDisponiblesAsync(usuarioId);

 
            var dtos = envios.Select(e => Mapear(e)).ToList();

            return Ok(dtos);
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al obtener las oportunidades.");
        }
    }

    [HttpGet("oportunidades/{id:int}")]
    public async Task<IActionResult> ObtenerOportunidadPorIdApi(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var envio = await _envioService.ObtenerDetalleOportunidadAsync(id, usuarioId);

        return envio == null ? NotFound() : Ok(envio);
    }



    /// <summary>
    /// Método privado: Extrae el ID del usuario desde el JWT (Claims).
    /// CLAVE: Nunca confiar en datos del cliente, siempre extraer desde tokens seguros.
    /// </summary>
    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        // Obtener usuarioId del JWT, no de parámetros o formularios
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out usuarioId);
    }

    /// <summary>
    /// Convierte una entidad Envio a DTO (Data Transfer Object).
    /// CLAVE: Mantener separación entre entidades de persistencia y contratos REST.
    /// </summary>
    private static EnvioDto Mapear(Envio envio)
    {
        // Convertir entidad a DTO: solo propiedades públicas, agregar lógica de negocio
        return new EnvioDto
        {
            Id = envio.Id,
            OrigenDir = envio.OrigenDir,
            DestinoDir = envio.DestinoDir,
            FechaLimite = envio.FechaLimite,
            PesoKg = envio.PesoKg,
            Tamano = envio.Tamano,
            Descripcion = envio.Descripcion ?? string.Empty,
            Monto = envio.Monto,
            Estado = envio.Estado,
            CreadoEn = envio.CreadoEn,
            EstadoNombre = ObtenerNombreEstado((EstadoEnvio)envio.Estado),
            // CLAVE: Calcular permisos en la API (no confiar en cliente)
            PuedeCancelar = (EstadoEnvio)envio.Estado is
                EstadoEnvio.BuscandoConductor or EstadoEnvio.ConductorAsignado,
            PuedeEliminar = (EstadoEnvio)envio.Estado is EstadoEnvio.Cancelado
        };
    }

    /// <summary>
    /// Convierte un enum EstadoEnvio a su nombre legible
    /// </summary>
    private static string ObtenerNombreEstado(EstadoEnvio estado) => estado switch
    {
        EstadoEnvio.BuscandoConductor => "Buscando conductor",
        EstadoEnvio.ConductorAsignado => "Conductor asignado",
        EstadoEnvio.Retirado => "Retirado",
        EstadoEnvio.EnCamino => "En camino",
        EstadoEnvio.Entregado => "Entregado",
        EstadoEnvio.Cancelado => "Cancelado",
        _ => "Estado desconocido"
    };

}
