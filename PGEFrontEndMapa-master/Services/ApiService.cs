using IntegrarMapa.Config;
using IntegrarMapa.Models;
using Mapsui.Providers.Wms;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

namespace IntegrarMapa.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl)
        };
    }

    //login
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var dto = new LoginDto
        {
            Email = email,
            Password = password
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/auth/login", dto);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<LoginResponse>();

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error de conexión: {ex.Message}");
            return null;
        }
    }

    //register
    public async Task<bool> RegisterAsync(string nombre, string apellido, string email, string username, string password)
    {
        var nuevoUsuario = new
        {
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Username = username,
            Password = password
        };

        var json = JsonSerializer.Serialize(nuevoUsuario);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync($"{ApiConfig.BaseUrl}/auth/register", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Cambiar contraseña
    public async Task<bool> ChangePasswordAsync(string email, string oldPassword, string newPassword)
    {
        var dto = new ChangePasswordDto
        {
            Email = email,
            OldPassword = oldPassword,
            NewPassword = newPassword
        };

        try
        {
            var response = await _http.PostAsJsonAsync("/auth/change-password", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error de conexión: {ex.Message}");
            return false;
        }
    }


}
