using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Servicios.Rutas;

/// <summary>
/// Implementación real del proveedor de rutas sobre la API de TomTom.
/// La API key se lee de configuración y nunca se registra en los logs.
/// </summary>
public sealed class TomTomRutaService : IRutaService
{
    private const string RutaCalculo = "routing/1/calculateRoute/{0}/json";
    private const string RutaGeocodificacion = "search/2/geocode/{0}.json";

    // El tráfico en vivo haría variar distancias y tiempos según la hora,
    // y el optimizador tiene que ser determinístico.
    // Los índices continúan la numeración de la plantilla de la ruta: {0} es
    // el recorrido y {1} la API key.
    private const string ParametrosCalculo =
        "routeRepresentation=summaryOnly&traffic=false&routeType=fastest&key={1}";

    // {0} es la dirección, {1} el país y {2} la API key.
    private const string ParametrosGeocodificacion =
        "countrySet={1}&limit=1&key={2}";

    private const int IntentosMaximos = 2;
    private static readonly TimeSpan EsperaEntreIntentos = TimeSpan.FromMilliseconds(400);

    private readonly HttpClient _httpClient;
    private readonly RutasOptions _opciones;

    public TomTomRutaService(HttpClient httpClient, IOptions<RutasOptions> opciones)
    {
        _httpClient = httpClient;
        _opciones = opciones.Value;

        if (string.IsNullOrWhiteSpace(_opciones.ApiKey))
        {
            throw new InvalidOperationException(
                "Falta configurar Rutas:ApiKey para usar el proveedor TomTom.");
        }
    }

    public async Task<CoordenadaDto?> GeocodificarAsync(
        string direccion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(direccion))
        {
            return null;
        }

        var url = string.Format(
            CultureInfo.InvariantCulture,
            RutaGeocodificacion + "?" + ParametrosGeocodificacion,
            Uri.EscapeDataString(direccion),
            Uri.EscapeDataString(_opciones.PaisBusqueda),
            Uri.EscapeDataString(_opciones.ApiKey));

        using var respuesta = await EnviarAsync(url, cancellationToken);
        using var documento = await LeerJsonAsync(respuesta, cancellationToken);

        if (!documento.RootElement.TryGetProperty("results", out var resultados)
            || resultados.ValueKind != JsonValueKind.Array
            || resultados.GetArrayLength() == 0)
        {
            return null;
        }

        var posicion = resultados[0].GetProperty("position");

        return new CoordenadaDto(
            Redondear(posicion.GetProperty("lat").GetDouble()),
            Redondear(posicion.GetProperty("lon").GetDouble()));
    }

    public async Task<RutaDto> CalcularRutaAsync(
        IReadOnlyList<CoordenadaDto> puntos,
        CancellationToken cancellationToken = default)
    {
        if (puntos is null || puntos.Count < 2)
        {
            throw new ArgumentException(
                "Se necesitan al menos dos puntos para calcular una ruta.",
                nameof(puntos));
        }

        var recorrido = string.Join(':', puntos.Select(FormatearPunto));

        var url = string.Format(
            CultureInfo.InvariantCulture,
            RutaCalculo + "?" + ParametrosCalculo,
            recorrido,
            Uri.EscapeDataString(_opciones.ApiKey));

        using var respuesta = await EnviarAsync(url, cancellationToken);
        using var documento = await LeerJsonAsync(respuesta, cancellationToken);

        if (!documento.RootElement.TryGetProperty("routes", out var rutas)
            || rutas.GetArrayLength() == 0)
        {
            throw new ProveedorRutasException(
                "El proveedor de rutas no devolvió ningún recorrido para los puntos enviados.");
        }

        var resumen = rutas[0].GetProperty("summary");
        var metros = resumen.GetProperty("lengthInMeters").GetDouble();
        var segundos = resumen.GetProperty("travelTimeInSeconds").GetDouble();

        var duracionMin = (int)Math.Ceiling(segundos / 60d);

        return new RutaDto(
            Redondear(metros / 1000d),
            duracionMin < 1 ? 1 : duracionMin,
            Geometria: null);
    }

    // Un reintento simple alcanza para los cortes transitorios del proveedor.
    private async Task<HttpResponseMessage> EnviarAsync(string url, CancellationToken cancellationToken)
    {
        for (var intento = 1; intento <= IntentosMaximos; intento++)
        {
            HttpResponseMessage respuesta;

            try
            {
                respuesta = await _httpClient.GetAsync(url, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProveedorRutasException(
                    "El proveedor de rutas no respondió a tiempo.");
            }
            catch (HttpRequestException ex)
            {
                throw new ProveedorRutasException(
                    "No se pudo conectar con el proveedor de rutas.", ex);
            }

            if (respuesta.IsSuccessStatusCode)
            {
                return respuesta;
            }

            var codigo = respuesta.StatusCode;
            respuesta.Dispose();

            var reintentable = codigo == HttpStatusCode.TooManyRequests
                               || (int)codigo >= 500;

            if (!reintentable || intento == IntentosMaximos)
            {
                throw new ProveedorRutasException(
                    $"El proveedor de rutas respondió {(int)codigo} ({codigo}).");
            }

            await Task.Delay(EsperaEntreIntentos, cancellationToken);
        }

        throw new ProveedorRutasException(
            "El proveedor de rutas no respondió correctamente.");
    }

    private static async Task<JsonDocument> LeerJsonAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var contenido = await respuesta.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(contenido, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new ProveedorRutasException(
                "El proveedor de rutas devolvió una respuesta que no se pudo interpretar.", ex);
        }
    }

    private static string FormatearPunto(CoordenadaDto punto) => string.Format(
        CultureInfo.InvariantCulture,
        "{0},{1}",
        punto.Lat,
        punto.Lng);

    private static decimal Redondear(double valor) =>
        Math.Round((decimal)valor, 2, MidpointRounding.AwayFromZero);
}
