using RuteaYa.Entidades;

namespace RuteaYa.Helpers
{
    // Clases de Bootstrap asociadas al estado de un envío, para no repetir el mapeo en cada vista.
    public static class EstadoEnvioBadge
    {
        public static string ObtenerClase(EstadoEnvio estado) => estado switch
        {
            EstadoEnvio.ConductorAsignado or EstadoEnvio.Retirado => "bg-primary-subtle text-primary-emphasis",
            EstadoEnvio.EnCamino => "bg-info-subtle text-info-emphasis",
            EstadoEnvio.Entregado => "bg-success-subtle text-success-emphasis",
            EstadoEnvio.Cancelado => "bg-danger-subtle text-danger-emphasis",
            _ => "bg-secondary-subtle text-secondary-emphasis"
        };
    }
}
