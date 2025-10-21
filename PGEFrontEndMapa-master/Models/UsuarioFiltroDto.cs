namespace IntegrarMapa.Models
{
    public class UsuarioFiltroDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}