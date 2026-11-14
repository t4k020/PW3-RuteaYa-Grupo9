namespace RuteaYa.Entidades;

// Traduce el estado de un envío al texto que se muestra en la interfaz.
// Está centralizado para que la API, los servicios y el MVC usen siempre el mismo nombre.
public static class EstadoEnvioExtensions
{
    public static string ObtenerNombre(this EstadoEnvio estado) => estado switch
    {
        EstadoEnvio.BuscandoConductor => "Buscando conductor",
        EstadoEnvio.ConductorAsignado => "Conductor asignado",
        EstadoEnvio.Retirado => "Retirado",
        EstadoEnvio.EnCamino => "En camino",
        EstadoEnvio.Entregado => "Entregado",
        EstadoEnvio.Cancelado => "Cancelado",
        _ => "Estado desconocido"
    };
}
