namespace IntegrarMapa.Models
{
    public class IncidenciaDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty; // ← NUEVA propiedad
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public int EstadoId { get; set; }  // ← Mantener como int
        public string Estado { get; set; } = string.Empty; // ← También mantener como string
        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string? IconoUrl { get; set; }

        // Propiedades computadas
        public DateTime Fecha => Timestamp.DateTime;
        public string Titulo => Descripcion;

        // Propiedad para colores según el estado
        public string EstadoColor
        {
            get
            {
                return Estado?.ToLower() switch
                {
                    "nueva" => "#28a745",        // Verde
                    "en_proceso" => "#ffc107",   // Amarillo  
                    "resuelta" => "#17a2b8",     // Azul
                    "cerrada" => "#6c757d",      // Gris
                    _ => "#6c757d"               // Gris por defecto
                };
            }
        }
    }
}