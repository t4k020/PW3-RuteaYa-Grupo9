namespace RuteaYa.Servicios.Dtos
{
    // Parada intermedia de un viaje, ya ordenada según el recorrido.
    public class ParadaDto
    {
        public int Id { get; set; }
        public int Orden { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
    }
}
