using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ApiPotencialController : ControllerBase
{
    private readonly IPotencialService _potencial;
    public ApiPotencialController(IPotencialService potencial) => _potencial = potencial;

    [HttpGet("mensual")]
    public async Task<IActionResult> Mensual([FromQuery] int anio, [FromQuery] int mes, [FromQuery] string criterio = "ganancia", CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return Unauthorized();
        try { return Ok(await _potencial.CalcularAsync(usuarioId, anio, mes, criterio, cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
    }
}
