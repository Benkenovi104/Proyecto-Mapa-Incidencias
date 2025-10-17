namespace IntegrarMapa.Models
{

    public class CreateIncidentDto
    {
        public int UserId { get; set; }
        public int CategoriaId { get; set; }
        public string? Descripcion { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string? FotoUrl { get; set; }
    }
}