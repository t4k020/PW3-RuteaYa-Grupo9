using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;


namespace RuteaYa.Controllers
{
    public class ViajeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiRoute = "api/ApiViaje"; 

        public ViajeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        // GET: Viaje/Index
        public async Task<IActionResult> Index(string filtro = "activos")
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");

            string queryParam = "";
            if (filtro == "activos")
            {
                queryParam = "?soloActivo=true";
            }
            else if (filtro == "inactivos")
            {
                queryParam = "?soloActivo=false";
            }


            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}{queryParam}");

                if (respuesta.IsSuccessStatusCode)
                {
                
                    var viajesDto = await respuesta.Content.ReadFromJsonAsync<List<RuteaYa.Servicios.Dtos.ViajeListadoDto>>() 
                        ?? new List<RuteaYa.Servicios.Dtos.ViajeListadoDto>();
                    ViewBag.Filtro = filtro; // Pasamos el filtro a la vista
                    return View(viajesDto);
                }

                TempData["Error"] = "No se pudieron cargar los viajes.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            ViewBag.FiltroActual = filtro; // Pasamos el filtro actual a la vista
            return View(new List<ViajeListadoDto>());
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Viaje modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.PostAsJsonAsync(ApiRoute, modelo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Viaje creado con éxito!";
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
        public async Task<IActionResult> Detalle(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    var viaje = await respuesta.Content.ReadFromJsonAsync<ViajeDetalleDto>();
                    return View(viaje);
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró este viaje.";
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

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    var viaje = await respuesta.Content.ReadFromJsonAsync<Viaje>();
                    return View(viaje);
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje a editar.";
                    return RedirectToAction("Index");
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Viaje modelo)
        {
            if (id != modelo.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.PutAsJsonAsync($"{ApiRoute}/{id}", modelo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Viaje actualizado con éxito!";
                    return RedirectToAction("Index");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje a actualizar.";
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError(string.Empty, "No tienes autorización para realizar esta acción.");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.DeleteAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Viaje eliminado con éxito!";
                    return RedirectToAction("Index");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje a eliminar.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");

            try
            {
                HttpResponseMessage respuesta = await client.PostAsync($"{ApiRoute}/{id}/desactivar", null);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Viaje desactivado con éxito!";
                    return RedirectToAction("Index");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje a desactivar.";
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
