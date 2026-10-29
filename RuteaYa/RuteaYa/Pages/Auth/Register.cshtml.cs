using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RuteaYa.Pages.Auth
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu nombre.")]
        public string Nombre { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu apellido.")]
        public string Apellido { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu email.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá una contraseña.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;

        private readonly HttpClient _httpClient;

        public RegisterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RuteaYaApi");
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return Page();

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "Api/Auth/Register",
                    new
                    {
                        Nombre,
                        Apellido,
                        Email,
                        PasswordHash
                    },
                    cancellationToken);

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync(
                        cancellationToken);

                    string? mensaje = null;
                    bool registrado = false;

                    try
                    {
                        using var json = JsonDocument.Parse(body);
                        var root = json.RootElement;

                        if (root.TryGetProperty("mensaje", out var mensajeJson))
                            mensaje = mensajeJson.GetString();

                        if (root.TryGetProperty("registrado", out var registradoJson))
                        {
                            registrado = registradoJson.ValueKind == JsonValueKind.True;
                        }
                    }
                    catch (JsonException)
                    {
                        // La API también podría devolver un error como texto plano.
                        mensaje = body;
                    }

                    if (response.IsSuccessStatusCode && registrado)
                    {
                        TempData["RegisterSuccess"] =
                            mensaje ?? "Registro exitoso. Ya podés iniciar sesión.";

                        return RedirectToPage("/Auth/Login");
                    }

                    ModelState.AddModelError(
                        string.Empty,
                        mensaje ?? "No se pudo completar el registro.");

                    return Page();
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo conectar con el servicio de registro.");

                return Page();
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El servicio tardó demasiado en responder.");

                return Page();
            }
        }
    }
}
