using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios;

namespace RuteaYa.Servicios.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios")]
public class ApiUsuarioController : ControllerBase
{
    private readonly UsuarioService _usuarios;

    public ApiUsuarioController(UsuarioService usuarios) => _usuarios = usuarios;

    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerPropio()
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        var usuario = await _usuarios.ObtenerPropioAsync(id);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPut("me")]
    public async Task<IActionResult> ActualizarPropio([FromBody] ActualizarUsuarioRequest solicitud)
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            return await _usuarios.ActualizarPropioAsync(id, solicitud) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "No se pudo actualizar el perfil.", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpDelete("me")]
    public async Task<IActionResult> EliminarPropio()
    {
        if (!TryGetUserId(out var id)) return Unauthorized();
        try
        {
            return await _usuarios.EliminarPropioAsync(id) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "No se puede eliminar el perfil.", Detail = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    private bool TryGetUserId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
