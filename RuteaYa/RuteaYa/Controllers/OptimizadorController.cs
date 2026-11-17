using Microsoft.AspNetCore.Mvc;
using RuteaYa.Models;
using RuteaYa.Servicios.Dtos;
using System.Globalization;
using System.Text.Json;

namespace RuteaYa.Controllers
{
    public class OptimizadorController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiRoute = "api/ApiOptimizador";
        private const string ApiViajeRoute = "api/ApiViaje";

        public OptimizadorController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // Pantalla del optimizador: el usuario elige viaje y fecha, y se muestra la mejor combinación.
        [HttpGet]
        public async Task<IActionResult> Index(int? viajeId = null, DateTime? fecha = null)
        {
            var modelo = new OptimizadorViewModel
            {
                ViajeId = viajeId,
                Fecha = (fecha ?? DateTime.Today).Date,
                Viajes = await ObtenerViajesActivosAsync()
            };

            // Solo se calcula cuando el usuario ya eligió viaje y fecha.
            if (viajeId.HasValue && fecha.HasValue)
            {
                modelo.Resultado = await CalcularAsync(viajeId.Value, modelo.Fecha);
            }

            return View(modelo);
        }

        // Resumen de la combinación elegida, antes de reservar.
        [HttpGet]
        public async Task<IActionResult> Confirmar(int viajeId, DateTime fecha, List<int> ids)
        {
            var volver = RutaIndex(viajeId, fecha);

            if (ids == null || ids.Count == 0)
            {
                TempData["Error"] = "Elegí una combinación para confirmar.";
                return RedirectToAction(nameof(Index), volver);
            }

            // Se recalcula en la API: no se confía en los datos que vuelven del navegador.
            var resultado = await CalcularAsync(viajeId, fecha.Date);
            if (resultado == null)
            {
                return RedirectToAction(nameof(Index), volver);
            }

            var elegidos = ids.Distinct().OrderBy(id => id).ToList();
            var elegida = new[] { resultado.Recomendada }
                .Concat(resultado.Alternativas)
                .Where(c => c != null)
                .FirstOrDefault(c => c!.Paquetes.Select(p => p.Id).OrderBy(id => id).SequenceEqual(elegidos));

            if (elegida == null)
            {
                TempData["Error"] = "La combinación ya no está disponible. Recalculá la combinación.";
                return RedirectToAction(nameof(Index), volver);
            }

            return View(new ConfirmarCombinacionViewModel
            {
                ViajeId = viajeId,
                OrigenDir = resultado.OrigenDir,
                DestinoDir = resultado.DestinoDir,
                Fecha = fecha.Date,
                Combinacion = elegida
            });
        }

        // Reserva todos los envíos de la combinación y lleva a Mis transportes.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int viajeId, DateTime fecha, List<int> ids)
        {
            var volver = RutaIndex(viajeId, fecha);

            if (ids == null || ids.Count == 0)
            {
                TempData["Error"] = "Elegí una combinación para reservar.";
                return RedirectToAction(nameof(Index), volver);
            }

            var client = _httpClientFactory.CreateClient("ApiOptimizador");

            try
            {
                var solicitud = new ReservarCombinacionDto
                {
                    ViajeId = viajeId,
                    FechaViaje = fecha.Date,
                    EnviosIds = ids
                };

                HttpResponseMessage respuesta = await client.PostAsJsonAsync($"{ApiRoute}/reservar", solicitud);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = ids.Count == 1
                        ? "Reservaste 1 envío."
                        : $"Reservaste {ids.Count} envíos.";
                    return RedirectToAction("MisTransportes", "Viaje");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Por ejemplo: otro conductor tomó un envío, hay que recalcular.
                    TempData["Error"] = await LeerDetalleAsync(
                        respuesta, "No se pudo reservar la combinación.");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje seleccionado.";
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "No tienes autorización para realizar esta acción.";
                }
                else
                {
                    TempData["Error"] = "No se pudo reservar la combinación.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return RedirectToAction(nameof(Index), volver);
        }

        // ---------- Auxiliares ----------

        private static object RutaIndex(int viajeId, DateTime fecha)
        {
            return new
            {
                viajeId,
                fecha = fecha.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }

        private async Task<List<ViajeListadoDto>> ObtenerViajesActivosAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiViajeRoute}?soloActivo=true");

                if (respuesta.IsSuccessStatusCode)
                {
                    return await respuesta.Content.ReadFromJsonAsync<List<ViajeListadoDto>>()
                        ?? new List<ViajeListadoDto>();
                }

                TempData["Error"] = "No se pudieron cargar tus viajes.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return new List<ViajeListadoDto>();
        }

        // Devuelve null si la API no pudo calcular, y deja el motivo en TempData["Error"].
        private async Task<OptimizacionDto?> CalcularAsync(int viajeId, DateTime fecha)
        {
            var client = _httpClientFactory.CreateClient("ApiOptimizador");
            var fechaTexto = fecha.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync(
                    $"{ApiRoute}?viajeId={viajeId}&fecha={fechaTexto}");

                if (respuesta.IsSuccessStatusCode)
                {
                    return await respuesta.Content.ReadFromJsonAsync<OptimizacionDto>();
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    TempData["Error"] = await LeerDetalleAsync(
                        respuesta, "No se pudo calcular la combinación.");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje seleccionado.";
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "No tienes autorización para usar el optimizador.";
                }
                else
                {
                    TempData["Error"] = "No se pudo calcular la combinación.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return null;
        }

        // Muestra el detalle que devuelve la API, o un mensaje genérico si no se puede leer.
        private static async Task<string> LeerDetalleAsync(HttpResponseMessage respuesta, string mensajePorDefecto)
        {
            try
            {
                var detalle = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
                return string.IsNullOrWhiteSpace(detalle?.Detail) ? mensajePorDefecto : detalle.Detail;
            }
            catch (JsonException)
            {
                return mensajePorDefecto;
            }
        }
    }
}
