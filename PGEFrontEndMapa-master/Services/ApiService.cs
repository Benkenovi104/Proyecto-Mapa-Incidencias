using IntegrarMapa.Config;
using IntegrarMapa.Models;
using System.Net.Http.Json;

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
}
