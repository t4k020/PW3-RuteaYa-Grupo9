using Microsoft.AspNetCore.Mvc;
using RuteaYa.Entidades;
using RuteaYa.Servicios.Dtos;
using RuteaYa.Models;
using RuteaYa.Servicios;
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
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest
                         || respuesta.StatusCode == System.Net.HttpStatusCode.BadGateway)
                {
                    // La API explica el motivo: datos inválidos (400), dirección
                    // no ubicada (400) o proveedor de rutas caído (502).
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vincular(int envioId, int viajeId, DateTime fechaViaje)
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                // Estructura el objeto con las propiedades que espera el AsignarEnvioDto de la API
                var modelo = new { ViajeId = viajeId, FechaViaje = fechaViaje };

                // Envía la solicitud POST al nuevo endpoint de la API
                HttpResponseMessage respuesta = await client.PostAsJsonAsync($"{ApiRoute}/{envioId}/asignar", modelo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Envío aceptado y asignado a tu viaje con éxito!";
                    return RedirectToAction("MisTransportes", "Viaje");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    ProblemDetails? detalle = null;
                    try
                    {
                        detalle = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
                    }
                    catch (JsonException) { }

                    TempData["Error"] = detalle?.Detail ?? "El envío no pudo ser vinculado debido a una regla de negocio.";
                }
                else
                {
                    TempData["Error"] = "Ocurrió un error inesperado al procesar la asignación.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return RedirectToAction(nameof(DetallesOportunidad), new
            {
                id = envioId, viajeId,
                fecha = fechaViaje.ToString("yyyy-MM-dd") 
            });
        }
        [HttpGet]
        public async Task<IActionResult> Oportunidades(int? viajeId, DateTime? fecha, string orden = "ganancia")
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");
            var modelo = new OportunidadesViewModel
            {
                ViajeId = viajeId,
                Fecha = (fecha ?? DateTime.Today).Date,
                Orden = orden
            };

            try
            {
                var respuestaViajes = await client.GetAsync("api/ApiViaje?soloActivo=true");
                if (respuestaViajes.IsSuccessStatusCode)
                    modelo.Viajes = await respuestaViajes.Content.ReadFromJsonAsync<List<ViajeListadoDto>>() ?? new();

                if (!viajeId.HasValue)
                    return View(modelo);

                var ruta = $"{ApiRoute}/oportunidades?viajeId={viajeId}&fecha={modelo.Fecha:yyyy-MM-dd}&orden={Uri.EscapeDataString(orden)}";
                HttpResponseMessage respuesta = await client.GetAsync(ruta);

                if (respuesta.IsSuccessStatusCode)
                {
                    modelo.Resultado = await respuesta.Content.ReadFromJsonAsync<OportunidadesResultadoDto>();
                    return View(modelo);
                }
                else
                {
                    TempData["Error"] = "No se pudieron cargar las oportunidades.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return View(modelo);
        }

        // Pantalla 11 para los envíos elegidos en Oportunidades: resumen antes de reservar.
        [HttpGet]
        public async Task<IActionResult> ConfirmarSeleccion(
            int viajeId, DateTime fecha, List<int> ids, string orden = "ganancia")
        {
            var volver = new
            {
                viajeId,
                fecha = fecha.Date.ToString("yyyy-MM-dd"),
                orden
            };

            if (ids == null || ids.Count == 0)
            {
                TempData["Error"] = "Elegí al menos un envío para confirmar.";
                return RedirectToAction(nameof(Oportunidades), volver);
            }

            // Se vuelve a consultar a la API: no se confía en lo que vuelve del navegador.
            var resultado = await ObtenerOportunidadesParaConfirmarAsync(viajeId, fecha.Date, orden);
            if (resultado == null)
            {
                return RedirectToAction(nameof(Oportunidades), volver);
            }

            var elegidos = ids.Distinct().ToList();
            var seleccionadas = resultado.Oportunidades.Where(o => elegidos.Contains(o.Id)).ToList();

            if (seleccionadas.Count != elegidos.Count)
            {
                TempData["Error"] = "Otro conductor tomó un envío o ya no está disponible. Elegí de nuevo.";
                return RedirectToAction(nameof(Oportunidades), volver);
            }

            var pesoTotal = seleccionadas.Sum(o => o.PesoKg);
            var desvioTotal = seleccionadas.Sum(o => o.DesvioKm);

            if (seleccionadas.Count > resultado.CupoDisponible
                || pesoTotal > resultado.PesoDisponibleKg
                || desvioTotal > resultado.DesvioDisponibleKm)
            {
                TempData["Error"] = "La selección supera el cupo, el peso o el desvío disponibles del viaje.";
                return RedirectToAction(nameof(Oportunidades), volver);
            }

            var combinacion = new CombinacionDto
            {
                GananciaTotal = seleccionadas.Sum(o => o.Monto),
                PesoTotalKg = pesoTotal,
                DesvioTotalKm = desvioTotal,
                EsValida = true
            };

            foreach (var o in seleccionadas)
            {
                combinacion.Paquetes.Add(new PaqueteOptimizableDto
                {
                    Id = o.Id,
                    OrigenDir = o.OrigenDir,
                    DestinoDir = o.DestinoDir,
                    Descripcion = o.Descripcion,
                    PesoKg = o.PesoKg,
                    Monto = o.Monto,
                    DesvioKm = o.DesvioKm,
                    TiempoExtraMin = OptimizadorService.EstimarTiempoExtraMin(o.DesvioKm)
                });
            }

            // Reutiliza la pantalla de confirmación del optimizador; "Reservar" es el mismo POST.
            return View("~/Views/Optimizador/Confirmar.cshtml", new ConfirmarCombinacionViewModel
            {
                ViajeId = viajeId,
                OrigenDir = resultado.OrigenDir,
                DestinoDir = resultado.DestinoDir,
                Fecha = resultado.FechaViaje,
                DesdeOportunidades = true,
                Orden = orden,
                Combinacion = combinacion
            });
        }

        // Devuelve null si la API no pudo buscar, y deja el motivo en TempData["Error"].
        private async Task<OportunidadesResultadoDto?> ObtenerOportunidadesParaConfirmarAsync(
            int viajeId, DateTime fecha, string orden)
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");
            var ruta = $"{ApiRoute}/oportunidades?viajeId={viajeId}&fecha={fecha:yyyy-MM-dd}&orden={Uri.EscapeDataString(orden)}";

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync(ruta);

                if (respuesta.IsSuccessStatusCode)
                {
                    return await respuesta.Content.ReadFromJsonAsync<OportunidadesResultadoDto>();
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    ProblemDetails? detalle = null;
                    try
                    {
                        detalle = await respuesta.Content.ReadFromJsonAsync<ProblemDetails>();
                    }
                    catch (JsonException) { }

                    TempData["Error"] = string.IsNullOrWhiteSpace(detalle?.Detail)
                        ? "No se pudo confirmar la selección."
                        : detalle.Detail;
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el viaje seleccionado.";
                }
                else
                {
                    TempData["Error"] = "No se pudo confirmar la selección.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> DetallesOportunidad(int id, int? viajeId = null, DateTime? fecha = null)
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                if (viajeId.HasValue && fecha.HasValue)
                {
                    var ruta = $"{ApiRoute}/oportunidades/{id}/detalle?viajeId={viajeId}&fecha={fecha.Value:yyyy-MM-dd}";
                    var respuestaDetalle = await client.GetAsync(ruta);

                    if (respuestaDetalle.IsSuccessStatusCode)
                    {
                        var detalle = await respuestaDetalle.Content.ReadFromJsonAsync<DetalleOportunidadViajeDto>();
                        return View("DetalleOportunidadViaje", detalle);
                    }

                    TempData["Error"] = respuestaDetalle.StatusCode == System.Net.HttpStatusCode.NotFound
                        ? "No se encontró la oportunidad solicitada."
                        : "No se pudieron obtener los detalles de la oportunidad.";
                    return RedirectToAction(nameof(Oportunidades), new { viajeId, fecha = fecha.Value.ToString("yyyy-MM-dd") });
                }

                // --- flujo anterior (sin viaje elegido): se mantiene igual ---
                HttpResponseMessage respuestaEnvio = await client.GetAsync($"{ApiRoute}/oportunidades/{id}");

                if (!respuestaEnvio.IsSuccessStatusCode)
                {
                    TempData["Error"] = respuestaEnvio.StatusCode == System.Net.HttpStatusCode.NotFound
                        ? "No se encontró la oportunidad solicitada."
                        : "No se pudieron obtener los detalles de la oportunidad.";
                    return RedirectToAction(nameof(Oportunidades));
                }

                var envio = await respuestaEnvio.Content.ReadFromJsonAsync<EnvioDto>();

                HttpResponseMessage respuestaViajes = await client.GetAsync("api/ApiViaje?soloActivo=true");
                ViewBag.MisViajes = respuestaViajes.IsSuccessStatusCode
                    ? await respuestaViajes.Content.ReadFromJsonAsync<List<ViajeListadoDto>>() ?? new List<ViajeListadoDto>()
                    : new List<ViajeListadoDto>();

                return View("DetalleOportunidad", envio);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
                return RedirectToAction(nameof(Oportunidades));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                HttpResponseMessage respuesta = await client.GetAsync($"{ApiRoute}/{id}");

                if (respuesta.IsSuccessStatusCode)
                {
                    var envio = await respuesta.Content.ReadFromJsonAsync<EnvioDto>();

                    if (envio is null)
                    {
                        TempData["Error"] = "No se encontró el envío a editar.";
                        return RedirectToAction(nameof(Index));
                    }

                    // La API indica si el estado actual permite editar.
                    if (!envio.PuedeEditar)
                    {
                        TempData["Error"] = "El envío solo puede editarse mientras busca conductor.";
                        return RedirectToAction(nameof(Index));
                    }

                    ViewData["EnvioId"] = id;
                    return View(new CrearEnvioDto
                    {
                        OrigenDir = envio.OrigenDir,
                        DestinoDir = envio.DestinoDir,
                        FechaLimite = envio.FechaLimite,
                        PesoKg = envio.PesoKg,
                        Tamano = envio.Tamano,
                        Descripcion = envio.Descripcion,
                        Monto = envio.Monto
                    });
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el envío a editar.";
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "No tienes autorización para editar este envío.";
                }
                else
                {
                    TempData["Error"] = "No se pudo cargar el envío.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "No se pudo conectar con el servidor de la API.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CrearEnvioDto modelo)
        {
            ViewData["EnvioId"] = id;

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var client = _httpClientFactory.CreateClient("ApiEnvio");

            try
            {
                HttpResponseMessage respuesta = await client.PutAsJsonAsync($"{ApiRoute}/{id}", modelo);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = "¡Envío actualizado con éxito!";
                    return RedirectToAction(nameof(Index));
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["Error"] = "No se encontró el envío a editar.";
                    return RedirectToAction(nameof(Index));
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ModelState.AddModelError(string.Empty, "No tienes autorización para realizar esta acción.");
                }
                else if (respuesta.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
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

    }
}
