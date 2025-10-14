namespace Api.Dtos;

public class IncidentVm
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string? Descripcion { get; set; }
    public string? FotoUrl { get; set; }
    public string Estado { get; set; } = ""; 
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}