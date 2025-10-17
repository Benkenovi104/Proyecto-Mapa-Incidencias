namespace Api.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";

    public string? IconoUrl { get; set; } // ← nuevo campo

}
