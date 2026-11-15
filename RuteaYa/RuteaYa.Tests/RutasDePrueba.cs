using Microsoft.Extensions.Options;
using RuteaYa.Servicios.Servicios.Rutas;

namespace RuteaYa.Tests;

/// <summary>
/// Crea el servicio de rutas mock para los tests. Ningún test depende de
/// internet ni de una API key (§10 de AGENTS).
/// </summary>
internal static class RutasDePrueba
{
    public static IRutaService CrearMock() =>
        new RutaServiceMock(Options.Create(new RutasOptions()));
}
