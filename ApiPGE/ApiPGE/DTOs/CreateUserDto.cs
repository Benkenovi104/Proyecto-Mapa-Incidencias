namespace Api.Dtos;

public class CreateUserDto
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int RolId { get; set; } // ← Ahora es int (1 para vecino, 2 para administrador)
}