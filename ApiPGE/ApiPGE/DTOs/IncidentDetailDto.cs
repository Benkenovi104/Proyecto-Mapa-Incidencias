namespace Api.Dtos;

public class IncidentDetailDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public string Estado { get; set; } = "";
    public int EstadoId { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string UsuarioNombre { get; set; } = "";
    public string UsuarioEmail { get; set; } = "";
}