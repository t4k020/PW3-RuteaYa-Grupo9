using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Servicios.Servicios.Rutas;

/// <summary>
/// Implementación ficticia del proveedor de rutas. Permite desarrollar, hacer
/// demo y correr los tests sin API key ni conexión a internet.
/// El resultado es determinístico: la misma dirección siempre devuelve las
/// mismas coordenadas.
/// </summary>
public sealed class RutaServiceMock : IRutaService
{
    private const double RadioTierraKm = 6371d;
    private const int MinutosMinimos = 1;
    private const int DecimalesCoordenada = 6;

    // Caja que contiene a la Ciudad de Buenos Aires. Se usa para inventar una
    // coordenada estable cuando la dirección no está en el catálogo.
    private const decimal LatMinima = -34.70m;
    private const decimal LatMaxima = -34.53m;
    private const decimal LngMinima = -58.53m;
    private const decimal LngMaxima = -58.33m;
    private const decimal RangoHash = 1000m;

    // Direcciones habituales del seed y de las pruebas manuales, con
    // coordenadas aproximadas de AMBA.
    private static readonly IReadOnlyDictionary<string, CoordenadaDto> DireccionesConocidas =
        new (string Direccion, decimal Lat, decimal Lng)[]
        {
            ("Av. Rivadavia 1234, CABA", -34.608344m, -58.392612m),
            ("Av. Corrientes 5678, CABA", -34.587528m, -58.437509m),
            ("Av. Santa Fe 1000, CABA", -34.595595m, -58.384061m),
            ("Av. Pueyrredón 500, CABA", -34.598012m, -58.400123m),
            ("Av. Belgrano 900, CABA", -34.613043m, -58.381012m),
            ("Av. Belgrano 1500, CABA", -34.615098m, -58.387210m),
            ("Av. Independencia 1800, CABA", -34.620972m, -58.390431m),
            ("Av. Córdoba 1200, CABA", -34.598941m, -58.390112m),
            ("Av. San Juan 2400, CABA", -34.628015m, -58.390584m),
            ("Av. Las Heras 1500, CABA", -34.588512m, -58.400230m),
            ("Av. Scalabrini Ortiz 2200, CABA", -34.580071m, -58.430102m),
            ("Av. Callao 600, CABA", -34.603471m, -58.392011m),
            ("Av. La Plata 1500, CABA", -34.626540m, -58.426280m),
            ("Av. Directorio 3000, CABA", -34.641210m, -58.478910m),
            ("Plaza de Mayo, CABA", -34.608300m, -58.371200m),
            ("Cabildo 2000, CABA", -34.562700m, -58.459000m),
            ("Corrientes 1000, CABA", -34.602500m, -58.385000m),
            ("Gaona 2500, CABA", -34.625000m, -58.470000m),
            ("Calle Falsa 123, Morón", -34.653400m, -58.619200m),
            ("Mitre 450, San Justo", -34.685100m, -58.558700m),
            ("Ruta 3 Km 28, La Matanza", -34.750000m, -58.590000m)
        }
        .Select(fila => new KeyValuePair<string, CoordenadaDto>(
            Normalizar(fila.Direccion),
            new CoordenadaDto(fila.Lat, fila.Lng)))
        .ToDictionary(par => par.Key, par => par.Value, StringComparer.Ordinal);

    private readonly MockRutasOptions _opciones;

    public RutaServiceMock(IOptions<RutasOptions> opciones)
    {
        _opciones = opciones.Value.Mock;

        if (_opciones.FactorSinuosidad <= 0)
        {
            throw new InvalidOperationException(
                "La configuración Rutas:Mock:FactorSinuosidad debe ser mayor que cero.");
        }

        if (_opciones.VelocidadKmH <= 0)
        {
            throw new InvalidOperationException(
                "La configuración Rutas:Mock:VelocidadKmH debe ser mayor que cero.");
        }
    }

    public Task<CoordenadaDto?> GeocodificarAsync(
        string direccion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(direccion) || !direccion.Any(char.IsLetter))
        {
            return Task.FromResult<CoordenadaDto?>(null);
        }

        var normalizada = Normalizar(direccion);

        if (DireccionesConocidas.TryGetValue(normalizada, out var conocida))
        {
            return Task.FromResult<CoordenadaDto?>(conocida);
        }

        return Task.FromResult<CoordenadaDto?>(InventarCoordenada(normalizada));
    }

    public Task<RutaDto> CalcularRutaAsync(
        IReadOnlyList<CoordenadaDto> puntos,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (puntos is null || puntos.Count < 2)
        {
            throw new ArgumentException(
                "Se necesitan al menos dos puntos para calcular una ruta.",
                nameof(puntos));
        }

        var distanciaLineal = 0d;

        for (var indice = 1; indice < puntos.Count; indice++)
        {
            distanciaLineal += DistanciaHaversineKm(puntos[indice - 1], puntos[indice]);
        }

        var distanciaKm = distanciaLineal * _opciones.FactorSinuosidad;
        var duracionMin = (int)Math.Ceiling(distanciaKm / _opciones.VelocidadKmH * 60d);

        if (duracionMin < MinutosMinimos)
        {
            duracionMin = MinutosMinimos;
        }

        var ruta = new RutaDto(
            Math.Round((decimal)distanciaKm, 2, MidpointRounding.AwayFromZero),
            duracionMin,
            Geometria: null);

        return Task.FromResult(ruta);
    }

    // Cuando la dirección no está en el catálogo se inventa una coordenada
    // estable dentro de CABA, derivada del texto. Alcanza para que el desvío
    // y las distancias del entorno de desarrollo sean siempre los mismos.
    private static CoordenadaDto InventarCoordenada(string direccionNormalizada)
    {
        var hashLatitud = HashEstable(direccionNormalizada);
        var hashLongitud = HashEstable(direccionNormalizada + "#longitud");

        var lat = LatMinima + hashLatitud % RangoHash / RangoHash * (LatMaxima - LatMinima);
        var lng = LngMinima + hashLongitud % RangoHash / RangoHash * (LngMaxima - LngMinima);

        return new CoordenadaDto(
            Math.Round(lat, DecimalesCoordenada, MidpointRounding.AwayFromZero),
            Math.Round(lng, DecimalesCoordenada, MidpointRounding.AwayFromZero));
    }

    // Hash FNV-1a: String.GetHashCode cambia entre ejecuciones, este no.
    private static uint HashEstable(string texto)
    {
        const uint offset = 2166136261;
        const uint primo = 16777619;

        var hash = offset;

        foreach (var caracter in texto)
        {
            hash ^= caracter;
            hash *= primo;
        }

        return hash;
    }

    private static double DistanciaHaversineKm(CoordenadaDto origen, CoordenadaDto destino)
    {
        var deltaLatitud = GradosARadianes((double)(destino.Lat - origen.Lat));
        var deltaLongitud = GradosARadianes((double)(destino.Lng - origen.Lng));

        var latitudOrigen = GradosARadianes((double)origen.Lat);
        var latitudDestino = GradosARadianes((double)destino.Lat);

        var senoLatitud = Math.Sin(deltaLatitud / 2);
        var senoLongitud = Math.Sin(deltaLongitud / 2);

        var a = senoLatitud * senoLatitud
                + Math.Cos(latitudOrigen) * Math.Cos(latitudDestino) * senoLongitud * senoLongitud;

        var angulo = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RadioTierraKm * angulo;
    }

    private static double GradosARadianes(double grados) => grados * Math.PI / 180d;

    // Unifica mayúsculas, espacios y acentos para que las direcciones se
    // busquen igual aunque el usuario las escriba distinto.
    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var constructor = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                constructor.Append(caracter);
            }
        }

        var partes = constructor
            .ToString()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', partes);
    }
}
