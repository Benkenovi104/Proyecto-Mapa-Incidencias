public class IncidentAdminDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string FotoUrl { get; set; } = string.Empty;
    public int EstadoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? IconoUrl { get; set; }
}