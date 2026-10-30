
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace RuteaYa.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RuteaYaApi");
        }

        // GET: Auth/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            var redireccion = RedirigirSiAutenticado();
            if (redireccion != null)
                return redireccion; 
            return View();
        }

        // POST: Auth/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ingresá tu correo y contraseña.");

                return View();
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "Api/Auth/Login",
                    new
                    {
                        email,
                        passwordHash = password
                    },
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Email o contraseña incorrectos.");

                    return View();
                }

                var contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                using var json = JsonDocument.Parse(contenido);

                var root = json.RootElement;

                var id = root.GetProperty("id").ToString();
                var nombre = root.GetProperty("nombre").GetString();
                var correo = root.GetProperty("email").GetString();

                if (string.IsNullOrWhiteSpace(id) ||
                    string.IsNullOrWhiteSpace(nombre) ||
                    string.IsNullOrWhiteSpace(correo))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "La respuesta de la API está incompleta.");

                    return View();
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, id),
                    new(ClaimTypes.Name, nombre),
                    new(ClaimTypes.Email, correo)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo conectar con la API.");

                return View();
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La API devolvió un JSON inválido.");

                return View();
            }
        }

        // GET: Auth/Register
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            var redireccion = RedirigirSiAutenticado();
            if (redireccion != null)
                return redireccion; 
            return View();
        }

        // POST: Auth/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string nombre,
            string apellido,
            string email,
            string passwordHash,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(apellido) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(passwordHash))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Todos los campos son obligatorios.");

                return View();
            }

            if (passwordHash.Length < 8)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La contraseña debe tener al menos 8 caracteres.");

                return View();
            }

            try
            {
                var respuesta = await _httpClient.PostAsJsonAsync(
                    "Api/Auth/Register",
                    new
                    {
                        nombre,
                        apellido,
                        email,
                        passwordHash
                    },
                    cancellationToken);

                if (respuesta.IsSuccessStatusCode)
                {
                    TempData["RegisterSuccess"] =
                        "¡Cuenta creada correctamente! Ya podés iniciar sesión.";

                    return RedirectToAction("Login", "Auth");
                }

                if (respuesta.StatusCode == HttpStatusCode.BadRequest)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Los datos enviados no son válidos o el correo ya está registrado.");
                }
                else
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Ocurrió un error al comunicarse con el servidor.");
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo conectar con el servidor de la API.");
            }

            return View();
        }

        // POST: Auth/Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Auth");
        }

        private IActionResult RedirigirSiAutenticado()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return null!;
        }
    }
}
