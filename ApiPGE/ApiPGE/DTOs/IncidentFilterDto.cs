namespace Api.Dtos;

public class IncidentFilterDto
{
    public int? CategoriaId { get; set; }
    public DateTimeOffset? Desde { get; set; }
    public DateTimeOffset? Hasta { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public double? Radius { get; set; }
    public int? Limit { get; set; } = 100;
}