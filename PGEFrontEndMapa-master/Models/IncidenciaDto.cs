namespace IntegrarMapa.Models
{
    public class IncidenciaDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty; // ← CAMBIAR de Titulo a Descripcion
        public double Lat { get; set; } // ← CAMBIAR de Latitud a Lat
        public double Lon { get; set; } // ← CAMBIAR de Longitud a Lon
        public int CategoriaId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string? IconoUrl { get; set; }

        // Propiedades para display
        public string CategoriaNombre { get; set; } = string.Empty;
        public DateTime Fecha => Timestamp.DateTime;

        // Propiedad computada para compatibilidad (opcional)
        public string Titulo => Descripcion; // ← Para mantener compatibilidad
        public double Latitud => Lat;        // ← Para mantener compatibilidad  
        public double Longitud => Lon;       // ← Para mantener compatibilidad
    }
}