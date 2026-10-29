using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace RuteaYa.Pages.Login
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RuteaYaApi");
        }

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu contraseña.")]
        public string Password { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "Api/Auth/Login",
                    new { email = Email, passwordHash = Password },
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Email o contraseña incorrectos.");
                    return Page();
                }

                using var json = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken),
                    cancellationToken: cancellationToken);

                var root = json.RootElement;

                // Adaptado al JSON: mensaje, id, nombre y email.
                var id = root.GetProperty("id").ToString();
                var nombre = root.GetProperty("nombre").GetString();
                var email = root.GetProperty("email").GetString();

                if (string.IsNullOrWhiteSpace(id) ||
                    string.IsNullOrWhiteSpace(nombre) ||
                    string.IsNullOrWhiteSpace(email))
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "La respuesta de la API está incompleta.");
                    return Page();
                }

                var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, id),
                new(ClaimTypes.Name, nombre),
                new(ClaimTypes.Email, email)
            };

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return RedirectToPage("/Index");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo conectar con la API.");
                return Page();
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La API devolvió una respuesta JSON inválida.");
                return Page();
            }
        }
    }
}