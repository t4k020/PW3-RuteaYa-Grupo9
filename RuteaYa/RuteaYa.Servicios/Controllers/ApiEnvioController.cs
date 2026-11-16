using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Servicios.Servicios.Rutas;

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
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
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
            // El servicio geocodifica las direcciones y asigna usuario,
            // estado y coordenadas: nunca se confía en datos del cliente.
            var nuevoEnvio = await _envioService.CrearEnvioAsync(
                solicitud,
                usuarioId,
                HttpContext.RequestAborted);

            // Retornar 201 Created con la URL del nuevo recurso
            return CreatedAtAction(
                nameof(ObtenerPorIdApi),
                new { id = nuevoEnvio.Id },
                Mapear(nuevoEnvio));
        }
        catch (ProveedorRutasException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "Servicio de rutas no disponible.",
                Detail = ex.Message,
                Status = StatusCodes.Status502BadGateway
            });
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

    /// PUT: Edita un envío (solo si pertenece al usuario y todavía busca conductor).
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ActualizarEnvioApi(int id, [FromBody] CrearEnvioDto solicitud)
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
            var datos = new Envio
            {
                OrigenDir = solicitud.OrigenDir,
                DestinoDir = solicitud.DestinoDir,
                FechaLimite = solicitud.FechaLimite,
                PesoKg = solicitud.PesoKg,
                Tamano = solicitud.Tamano,
                Descripcion = solicitud.Descripcion,
                Monto = solicitud.Monto
            };

            var actualizado = await _envioService.ActualizarEnvioAsync(id, datos, usuarioId);
            return actualizado ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No se puede editar el envío.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception)
        {
            return StatusCode(500, "Ocurrió un error interno al editar el envío.");
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
    public async Task<IActionResult> ObtenerOportunidadesApi([FromQuery] int viajeId, [FromQuery] DateTime fecha, [FromQuery] string orden = "ganancia", CancellationToken cancellationToken = default)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        try
        {

            var oportunidades = await _envioService.ObtenerOportunidadesParaViajeAsync(viajeId, fecha, usuarioId, orden, cancellationToken);
            return oportunidades is null ? NotFound() : Ok(oportunidades);
        }
        catch (InvalidOperationException ex) { return BadRequest(new ProblemDetails { Detail = ex.Message, Status = StatusCodes.Status400BadRequest }); }
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
            EstadoNombre = ((EstadoEnvio)envio.Estado).ObtenerNombre(),
            // CLAVE: Calcular permisos en la API (no confiar en cliente)
            PuedeEditar = (EstadoEnvio)envio.Estado is EstadoEnvio.BuscandoConductor,
            PuedeCancelar = (EstadoEnvio)envio.Estado is
                EstadoEnvio.BuscandoConductor or EstadoEnvio.ConductorAsignado,
            PuedeEliminar = (EstadoEnvio)envio.Estado is EstadoEnvio.Cancelado
        };
    }

}
