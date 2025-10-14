namespace IntegrarMapa.Models;

public class LoginResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string Rol { get; set; } = "";
    public DateTimeOffset LoginTime { get; set; }
}
