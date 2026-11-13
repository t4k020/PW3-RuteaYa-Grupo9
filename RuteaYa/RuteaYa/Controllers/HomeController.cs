using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            // Este cliente agrega automáticamente el JWT mediante JwtAuthorizationHandler.
            var client = _httpClientFactory.CreateClient("ApiHome");

            try
            {
                var response = await client.GetAsync("api/ApiHome", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var dashboard = await response.Content
                        .ReadFromJsonAsync<HomeDashboardDto>(cancellationToken);

                    if (dashboard is not null)
                    {
                        return View(dashboard);
                    }
                }

                // La vista sigue siendo utilizable aunque la API no responda.
                TempData["Error"] = "No se pudo cargar el resumen de tu cuenta.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return View(new HomeDashboardDto
            {
                NombreUsuario = User.Identity?.Name ?? string.Empty
            });
        }

        public async Task<IActionResult> Historial(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("ApiHome");
            try
            {
                var response = await client.GetAsync("api/ApiHome/historial", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    // Recibimos el HomeDashboardDto que ya conocemos
                    var historial = await response.Content.ReadFromJsonAsync<HomeDashboardDto>(cancellationToken);
                    if (historial is not null) return View(historial);
                }
            }
            catch (HttpRequestException) { }

            return View(new HomeDashboardDto());
        }
    }


}
