using RuteaYa.Servicios.Dtos;

namespace RuteaYa.Models;

// Modelo de la vista de detalle del paquete: combina los datos que devuelve
// la API con el origen de navegación, que es una decisión exclusiva del MVC.
public sealed class PaqueteDetalleViewModel
{
    public PaqueteDetalleDto Paquete { get; init; } = new();
    public bool VolverAMisTransportes { get; init; }
}
