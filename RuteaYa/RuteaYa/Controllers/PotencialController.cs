using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Controllers;

[Authorize]
public sealed class PotencialController : Controller
{
    private readonly IHttpClientFactory _clientes;
    public PotencialController(IHttpClientFactory clientes) => _clientes = clientes;

    public async Task<IActionResult> Mensual(int? anio, int? mes, string criterio = "ganancia", CancellationToken cancellationToken = default)
    {
        var fecha = DateTime.Today;
        var a = anio ?? fecha.Year; var m = mes ?? fecha.Month;
        try
        {
            var cliente = _clientes.CreateClient("ApiPotencial");
            var url = $"api/ApiPotencial/mensual?anio={a}&mes={m}&criterio={Uri.EscapeDataString(criterio)}";
            var respuesta = await cliente.GetAsync(url, cancellationToken);
            if (respuesta.IsSuccessStatusCode)
            {
                var modelo = await respuesta.Content.ReadFromJsonAsync<PotencialMensualDto>(cancellationToken);
                if (modelo is not null) return View(modelo);
            }
            TempData["Error"] = "No se pudo calcular el potencial mensual.";
        }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con la API."; }
        return View(new PotencialMensualDto { Anio = a, Mes = m, Criterio = criterio });
    }
}
