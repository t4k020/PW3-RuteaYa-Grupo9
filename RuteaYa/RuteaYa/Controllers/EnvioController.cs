using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios;
using RuteaYa.Entidades;

namespace RuteaYa.Controllers
{
    public class EnvioController : Controller
    {

    
        public IActionResult Index()
        {
            return View();
        }

        private readonly IHttpClientFactory _httpClientFactory;

        // Inyectamos IHttpClientFactory 
        public EnvioController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Envio modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Creamos el cliente utilizando el nombre que configuramos en Program.cs
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // petición POST h
                HttpResponseMessage respuesta = await client.PostAsJsonAsync("api/ApiEnvio", modelo);

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
                    ModelState.AddModelError(string.Empty, "Error en los datos enviados a la API.");
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
            // Creamos el cliente utilizando el nombre configurado en Program.cs
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Petición GET a la API incluyendo el ID en la ruta
                HttpResponseMessage respuesta = await client.GetAsync($"api/ApiEnvio/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    // Deserializamos el JSON recibido al modelo Envio
                    var envio = await respuesta.Content.ReadFromJsonAsync<Envio>();
                    return View(envio);
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontro este envio.";
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
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API.");
            }

            return View("Index");
        }
    }
}
