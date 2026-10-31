using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios.Servicios;
using System.Security.Claims;

namespace RuteaYa.Servicios.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApiHomeController : ControllerBase
{
    private readonly IHomeService _homeService;

    public ApiHomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Dtos.HomeDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        // El usuario se identifica con el claim generado al iniciar sesión.
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return Unauthorized();
        }

        var dashboard = await _homeService.ObtenerDashboardAsync(
            usuarioId,
            DateTime.Now,
            cancellationToken);

        return Ok(dashboard);
    }
}
