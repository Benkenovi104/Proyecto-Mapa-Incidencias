namespace IntegrarMapa.Models
{
    public class Incidencia
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? FotoUrl { get; set; } 
        public string Estado { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string? IconoUrl { get; set; }
    }
}
