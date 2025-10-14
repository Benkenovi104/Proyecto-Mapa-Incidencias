namespace Api.Dtos;

public class UpdateIncidentDto
{
    public int? CategoriaId { get; set; }
    public string? Descripcion { get; set; }
    public string? FotoUrl { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public int? EstadoId { get; set; }
}
