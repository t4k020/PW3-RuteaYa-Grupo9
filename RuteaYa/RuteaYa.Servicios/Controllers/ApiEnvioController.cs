using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ApiEnvioController : ControllerBase
{
    private readonly IEnvioService _envioService;

    public ApiEnvioController(IEnvioService envioService)
    {
        _envioService = envioService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EnvioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerMisEnvios()
    {
        // El listado se filtra en la API usando el usuario autenticado.
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var envios = await _envioService.ObtenerPorUsuarioAsync(usuarioId);
        return Ok(envios.Select(Mapear));
    }

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

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            // Se construye la entidad en la API para evitar over-posting.
            // UsuarioId y Estado no son datos confiables enviados por el cliente.
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
            var envio = await _envioService.ObtenerPorIdAsync(id, usuarioId);
            return envio == null ? NotFound() : Ok(Mapear(envio));
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al obtener el envío.");
        }
    }

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
            var eliminado = await _envioService.EliminarEnvioAsync(id, usuarioId);
            return eliminado ? NoContent() : NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al eliminar el envío.");
        }
    }

    [NonAction]
    public async Task<IActionResult> CrearEnvio(Envio envio)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var nuevoEnvio = await _envioService.CrearEnvioAsync(envio);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevoEnvio.Id }, nuevoEnvio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al procesar el envío.");
        }
    }

    [NonAction]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        try
        {
            var envio = await _envioService.ObtenerPorIdAsync(id);
            return envio == null ? NotFound() : Ok(envio);
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al obtener el envío.");
        }
    }

    [NonAction]
    public async Task<IActionResult> EliminarEnvio(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        try
        {
            var eliminado = await _envioService.EliminarEnvioAsync(id);
            return eliminado ? NoContent() : NotFound();
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al eliminar el envío.");
        }
    }

    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        // La identidad se obtiene del JWT, nunca de un parámetro del formulario o de la URL.
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out usuarioId);
    }

    private static EnvioDto Mapear(Envio envio)
    {
        // El mapeo mantiene la frontera entre las entidades de persistencia y los contratos REST.
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
            CreadoEn = envio.CreadoEn
        };
    }

}
