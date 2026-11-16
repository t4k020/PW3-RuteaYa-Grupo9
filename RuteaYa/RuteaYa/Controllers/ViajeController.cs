using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RuteaYa.Entidades;
using RuteaYa.Models;
using RuteaYa.Servicios.Dtos;
using System.Text.Json;

namespace RuteaYa.Controllers
{
    public class ViajeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiRoute = "api/ApiViaje";

        // La API responde en camelCase (JsonSerializerDefaults.Web).
        private static readonly JsonSerializerOptions OpcionesJson =
            new(JsonSerializerDefaults.Web);

        public ViajeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<IActionResult> Index(string filtro = "activos")
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            string queryParam = "";
            if (filtro == "activos") queryParam = "?soloActivo=true";
            else if (filtro == "inactivos") queryParam = "?soloActivo=false";

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}{queryParam}");
                if (respuesta.IsSuccessStatusCode)
                {
                    var viajesDto = await respuesta.Content.ReadFromJsonAsync<List<RuteaYa.Servicios.Dtos.ViajeListadoDto>>() ?? new List<RuteaYa.Servicios.Dtos.ViajeListadoDto>();
                    ViewBag.FiltroActual = filtro;
                    return View(viajesDto);
                }
                TempData["Error"] = "No se pudieron cargar los viajes.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }
            ViewBag.FiltroActual = filtro;
            return View(new List<ViajeListadoDto>());
        }

        [HttpGet]
        public IActionResult Crear() => View(new CrearViajeDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearViajeDto modelo)
        {
            if (!ModelState.IsValid) return View(modelo);
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.PostAsJsonAsync(ApiRoute, modelo);
                if (respuesta.IsSuccessStatusCode) { TempData["Mensaje"] = "¡Viaje creado con éxito!"; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) ModelState.AddModelError(string.Empty, "No tienes autorización para realizar esta acción.");
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest) await AgregarErroresDeLaApiAsync(respuesta, ModelState);
                else ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al comunicarse con el servidor.");
            }
            catch (HttpRequestException) { ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API."); }
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");
                if (respuesta.IsSuccessStatusCode) { var viaje = await respuesta.Content.ReadFromJsonAsync<ViajeDetalleDto>(); return View(viaje); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) { TempData["Error"] = "No se encontró este viaje."; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) { TempData["Error"] = "No tienes autorización para realizar esta acción."; return RedirectToAction("Index"); }
                else ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al comunicarse con el servidor.");
            }
            catch (HttpRequestException) { ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API."); }
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Viaje/Paquete/{viajeId:int}/{envioId:int}")]
        public async Task<IActionResult> Paquete(int viajeId, int envioId, OrigenNavegacionPaquete? origen = null)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{viajeId}/paquetes/{envioId}");
                if (respuesta.IsSuccessStatusCode)
                {
                    var paquete = await respuesta.Content.ReadFromJsonAsync<PaqueteDetalleDto>();
                    if (paquete is not null) return View(new PaqueteDetalleViewModel { Paquete = paquete, VolverAMisTransportes = origen == OrigenNavegacionPaquete.MisTransportes });
                    TempData["Error"] = "La API devolvió una respuesta vacía para el paquete.";
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) TempData["Error"] = "No se encontró el paquete asignado a este viaje.";
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) TempData["Error"] = "No tienes autorización para ver este paquete.";
                else TempData["Error"] = "No se pudo cargar el detalle del paquete.";
            }
            catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con el servidor de la API."; }
            catch (JsonException) { TempData["Error"] = "La respuesta del servidor no es válida."; }
            return RedirigirSegunOrigen(viajeId, origen);
        }

        private IActionResult RedirigirSegunOrigen(int viajeId, OrigenNavegacionPaquete? origen) => origen == OrigenNavegacionPaquete.MisTransportes ? RedirectToAction(nameof(MisTransportes)) : RedirectToAction(nameof(Detalle), new { id = viajeId });

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");
                if (respuesta.IsSuccessStatusCode)
                {
                    var viaje = await respuesta.Content.ReadFromJsonAsync<ViajeDetalleDto>();
                    if (viaje is null) { TempData["Error"] = "La API devolvió una respuesta vacía para el viaje."; return RedirectToAction("Index"); }
                    return View(AModeloEdicion(viaje));
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) { TempData["Error"] = "No se encontró el viaje a editar."; return RedirectToAction("Index"); }
            }
            catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con el servidor de la API."; }
            return RedirectToAction("Index");
        }

        private static CrearViajeDto AModeloEdicion(ViajeDetalleDto viaje) => new CrearViajeDto { OrigenDir = viaje.OrigenDir, DestinoDir = viaje.DestinoDir, HoraSalida = viaje.HoraSalida, HoraLlegada = viaje.HoraLlegada, DiasSemana = viaje.DiasSemanaNumeros, MaxPaquetes = viaje.MaxPaquetes, PesoMaxKg = viaje.PesoMaximoKg, DesvioMaxKm = viaje.DesvioMaxKm, Paradas = viaje.Paradas.OrderBy(p => p.Orden).Select(p => new ParadaViajeDto { Direccion = p.Direccion }).ToList() };

        // Convierte el error que devuelve la API (ValidationProblemDetails o
        // ProblemDetails) en un mensaje del ModelState, así el usuario ve el
        // motivo real: validación, dirección no ubicada o proveedor caído.
        private static async Task AgregarErroresDeLaApiAsync(
            HttpResponseMessage respuesta,
            ModelStateDictionary modelState)
        {
            // El contenido de la respuesta solo se puede leer una vez: se
            // guarda como texto y se intenta interpretar con los dos formatos.
            string contenido;
            try
            {
                contenido = await respuesta.Content.ReadAsStringAsync();
            }
            catch (ObjectDisposedException)
            {
                modelState.AddModelError(
                    string.Empty,
                    "Error en los datos enviados a la API.");
                return;
            }

            ValidationProblemDetails? detalle = null;
            try
            {
                detalle = JsonSerializer.Deserialize<ValidationProblemDetails>(
                    contenido, OpcionesJson);
            }
            catch (JsonException)
            {
            }

            var mensajes = detalle?.Errors
                .SelectMany(error => error.Value)
                .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
                .Distinct()
                .ToList();

            if (mensajes is { Count: > 0 })
            {
                modelState.AddModelError(string.Empty, string.Join(" ", mensajes));
                return;
            }

            ProblemDetails? problem = null;
            try
            {
                problem = JsonSerializer.Deserialize<ProblemDetails>(
                    contenido, OpcionesJson);
            }
            catch (JsonException)
            {
            }

            modelState.AddModelError(
                string.Empty,
                problem?.Detail
                ?? problem?.Title
                ?? "Error en los datos enviados a la API.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, CrearViajeDto modelo)
        {
            if (!ModelState.IsValid) return View(modelo);
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.PutAsJsonAsync($"{ApiRoute}/{id}", modelo);
                if (respuesta.IsSuccessStatusCode) { TempData["Mensaje"] = "¡Viaje actualizado con éxito!"; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) { TempData["Error"] = "No se encontró el viaje a actualizar."; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) ModelState.AddModelError(string.Empty, "No tienes autorización para realizar esta acción.");
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest || respuesta.StatusCode == System.Net.HttpStatusCode.BadGateway) await AgregarErroresDeLaApiAsync(respuesta, ModelState);
                else ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al comunicarse con el servidor.");
            }
            catch (HttpRequestException) { ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor de la API."); }
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
                if (respuesta.IsSuccessStatusCode) { TempData["Mensaje"] = "¡Viaje eliminado con éxito!"; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) TempData["Error"] = "No se encontró el viaje a eliminar.";
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) TempData["Error"] = "No tienes autorización para realizar esta acción.";
                else TempData["Error"] = "Ocurrió un error inesperado al comunicarse con el servidor.";
            }
            catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con el servidor de la API."; }
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
                if (respuesta.IsSuccessStatusCode) { TempData["Mensaje"] = "¡Viaje desactivado con éxito!"; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) TempData["Error"] = "No se encontró el viaje a desactivar.";
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) TempData["Error"] = "No tienes autorización para realizar esta acción.";
                else TempData["Error"] = "Ocurrió un error inesperado al comunicarse con el servidor.";
            }
            catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con el servidor de la API."; }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.PostAsync($"{ApiRoute}/{id}/activar", null);
                if (respuesta.IsSuccessStatusCode) { TempData["Mensaje"] = "¡Viaje activado con éxito!"; return RedirectToAction("Index"); }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound) TempData["Error"] = "No se encontró el viaje a activar.";
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized) TempData["Error"] = "No tienes autorización para realizar esta acción.";
                else TempData["Error"] = "Ocurrió un error inesperado al comunicarse con el servidor.";
            }
            catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con el servidor de la API."; }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> MisTransportes()
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/MisTransportes");
                if (respuesta.IsSuccessStatusCode)
                {
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var transportes = await respuesta.Content.ReadFromJsonAsync<List<ViajeDetalleDto>>(opciones) ?? new List<ViajeDetalleDto>();
                    return View(transportes);
                }
                TempData["Error"] = "No se pudieron cargar tus transportes.";
            }
            catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con el servidor."; }
            return View(new List<ViajeDetalleDto>());
        }

        [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> MarcarRetirado(int id) => await CambiarEstadoTransporte(id, "retirado");
        [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> MarcarEnCamino(int id) => await CambiarEstadoTransporte(id, "en-camino");
        [HttpPost] [ValidateAntiForgeryToken] public async Task<IActionResult> MarcarEntregado(int id) => await CambiarEstadoTransporte(id, "entregado");

        private async Task<IActionResult> CambiarEstadoTransporte(int id, string accion)
        {
            var client = _httpClientFactory.CreateClient("ApiViaje");
            try
            {
                var respuesta = await client.PostAsync($"{ApiRoute}/{id}/estado/{accion}", null);
                if (respuesta.IsSuccessStatusCode) TempData["Mensaje"] = "El estado del envío fue actualizado."; else TempData["Error"] = "No se pudo actualizar el estado del envío.";
            }
            catch (HttpRequestException) { TempData["Error"] = "Error de conexión con la API."; }
            return RedirectToAction(nameof(MisTransportes));
        }
    }
}