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

        // Lista todos los envíos con filtrado opcional por estado
        [HttpGet]
        public async Task<IActionResult> Index(EstadoEnvio? estado = null)
        {
            // Consume la API para obtener envíos (no accede directamente a BD)
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Construye la ruta con filtro de estado si se proporciona
                var ruta = estado.HasValue
                    ? $"{ApiRoute}?estado={Uri.EscapeDataString(estado.Value.ToString())}"
                    : ApiRoute;
                HttpResponseMessage respuesta = await client.GetAsync(ruta);

                if (respuesta.IsSuccessStatusCode)
                {
                    var envios = await respuesta.Content.ReadFromJsonAsync<List<EnvioDto>>()
                        ?? new List<EnvioDto>();
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

            return View(new List<EnvioDto>());
        }

        // Muestra formulario para crear nuevo envío
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        // Procesa la creación de nuevo envío
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearEnvioDto modelo)
        {
            // Valida modelo client-side antes de enviar a la API
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Envía el nuevo envío a la API
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

        

        // Obtiene los detalles de un envío específico
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            // API valida que el envío pertenezca al usuario autenticado
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Solicita los detalles del envío por ID
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    var envio = await respuesta.Content.ReadFromJsonAsync<EnvioDto>();
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
        public async Task<IActionResult> Cancelar(int id)
        {
            // Obtiene el cliente HTTP configurado para la API de envíos
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Realiza una solicitud POST al endpoint de cancelación de la API
                // El contenido es null porque solo se necesita el ID en la ruta
                HttpResponseMessage respuesta = await client.PostAsync(
                    $"{ApiRoute}/{id}/cancelar",
                    content: null);

               
                if (respuesta.IsSuccessStatusCode)
                {
                  
                    TempData["Mensaje"] = "El envío fue cancelado.";
                }
                
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var detalle = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
                    // Muestra el detalle del error o un mensaje genérico por defecto
                    TempData["Error"] = detalle?.Detail
                        ?? "El envío no puede cancelarse en su estado actual.";
                }
               
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el envío.";
                }
                
                else
                {
                    TempData["Error"] = "No se pudo cancelar el envío.";
                }
            }
            // Captura errores de conexión con la API (ej: servidor inaccesible)
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            
            return RedirectToAction(nameof(Index));
        }

        // Elimina un envio de la base de datos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Envia solicitud DELETE a la API para eliminar el envio con el ID especificado
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
