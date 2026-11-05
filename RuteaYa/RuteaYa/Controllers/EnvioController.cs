using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;
using System.Text.Json;

namespace RuteaYa.Controllers
{
    public class EnvioController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiRoute = "api/ApiEnvio"; 

        public EnvioController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // MVC no accede al DbContext: consume el listado de la API mediante HttpClient.
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync(ApiRoute);

                if (respuesta.IsSuccessStatusCode)
                {
                    var envios = await respuesta.Content.ReadFromJsonAsync<List<Envio>>() ?? new List<Envio>();
                    return View(envios);
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "No tienes autorización para ver los envíos.";
                }
                else
                {
                    TempData["Error"] = "No se pudieron cargar los envíos.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return View(new List<Envio>());
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearEnvioDto modelo)
        {
            // Esta validación permite mostrar los errores antes de realizar la petición HTTP.
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                HttpResponseMessage respuesta = await client.PostAsJsonAsync(ApiRoute, modelo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Envío creado con éxito!";
                    return RedirectToAction("Index");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError(string.Empty, "No tienes autorización para realizar esta acción.");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Se intenta mostrar el detalle de validación devuelto por la API.
                    ProblemDetails? detalle = null;
                    try
                    {
                        detalle = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
                    }
                    catch (JsonException)
                    {
                    }

                    ModelState.AddModelError(
                        string.Empty,
                        detalle?.Detail ?? "Error en los datos enviados a la API.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al comunicarse con el servidor.");
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API.");
            }

            return View(modelo);
        }

        

        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            // La API también valida que el envío pertenezca al usuario autenticado.
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    var envio = await respuesta.Content.ReadFromJsonAsync<Envio>();
                    return View(envio);
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró este envío.";
                    return RedirectToAction("Index");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "No tienes autorización para realizar esta acción.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al comunicarse con el servidor.");
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                HttpResponseMessage respuesta = await client.DeleteAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Envío eliminado con éxito!";
                    return RedirectToAction("Index");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el envío a eliminar.";
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "No tienes autorización para realizar esta acción.";
                }
                else
                {
                    TempData["Error"] = "Ocurrió un error inesperado al comunicarse con el servidor.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return RedirectToAction("Index");
        }
    }
}
