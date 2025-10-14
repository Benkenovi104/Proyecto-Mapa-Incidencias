using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace Api.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Column("user_id")]
    public int User_Id { get; set; }

    [Column("categoria_id")]
    public int Categoria_Id { get; set; }

    public string? Descripcion { get; set; }

    public Point Ubicacion { get; set; } = default!;

    [Column("foto_url")]
    public string? Foto_Url { get; set; }

    [Column("estado_id")]
    public int EstadoId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    // Navigation properties
    public Usuario Usuario { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;
    public EstadoIncidencia Estado { get; set; } = null!;
}
