using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Controllers;

/// <summary>
/// API REST del optimizador de oportunidades.
/// Todos los endpoints requieren autenticación (JWT).
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ApiOptimizadorController : ControllerBase
{
    private readonly IOptimizadorService _optimizadorService;

    public ApiOptimizadorController(IOptimizadorService optimizadorService)
    {
        _optimizadorService = optimizadorService;
    }

    /// <summary>
    /// GET: Calcula la mejor combinación de oportunidades para un viaje del usuario en una fecha.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(OptimizacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Optimizar(
        [FromQuery] int viajeId,
        [FromQuery] DateTime fecha,
        CancellationToken cancellationToken)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {
            var resultado = await _optimizadorService.OptimizarAsync(
                viajeId, fecha, usuarioId, cancellationToken);

            return resultado == null ? NotFound() : Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No se puede optimizar el viaje.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al calcular la combinación.");
        }
    }

    /// <summary>
    /// POST: Reserva todos los envíos de una combinación en un viaje del usuario.
    /// </summary>
    [HttpPost("reservar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reservar(
        [FromBody] ReservarCombinacionDto solicitud,
        CancellationToken cancellationToken)
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
            var reservado = await _optimizadorService.ReservarCombinacionAsync(
                solicitud.ViajeId,
                solicitud.FechaViaje,
                solicitud.EnviosIds,
                usuarioId,
                cancellationToken);

            return reservado ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No se puede reservar la combinación.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al reservar la combinación.");
        }
    }

    // El usuarioId sale siempre del JWT, nunca de parámetros ni formularios.
    private bool TryObtenerUsuarioId(out int usuarioId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out usuarioId);
    }
}
