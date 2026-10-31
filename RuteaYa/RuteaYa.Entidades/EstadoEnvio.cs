namespace RuteaYa.Entidades;

// Los valores coinciden con los tinyint ya utilizados por la base de datos.
public enum EstadoEnvio : byte
{
    Pendiente = 0,
    BuscandoConductor = 1,
    ConductorAsignado = 2,
    Retirado = 3,
    Entregado = 4,
    Cancelado = 5
}
