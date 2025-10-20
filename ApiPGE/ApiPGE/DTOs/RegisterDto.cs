namespace Api.Dtos;

public class RegisterDto
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Rol { get; set; } = "";
}
