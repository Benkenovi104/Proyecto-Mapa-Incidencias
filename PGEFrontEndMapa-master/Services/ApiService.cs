using IntegrarMapa.Config;
using IntegrarMapa.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

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

    // ======================
    // 🔐 LOGIN
    // ======================
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

    // ======================
    // 🧾 REGISTER
    // ======================
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

    // ======================
    // 🔒 CAMBIAR CONTRASEÑA
    // ======================
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

    // ======================
    // 🗂️ CATEGORÍAS
    // ======================
    public async Task<List<CategoriaDto>> GetCategoriasAsync()
    {
        try
        {
            var response = await _http.GetAsync("/categories");
            response.EnsureSuccessStatusCode();
            var categorias = await response.Content.ReadFromJsonAsync<List<CategoriaDto>>();
            return categorias ?? new List<CategoriaDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener categorías: {ex.Message}");
            return new List<CategoriaDto>();
        }
    }

    // ======================
    // 📍 INCIDENCIAS CERCANAS
    // ======================
    public async Task<List<IncidenciaDto>> GetIncidenciasAsync()
    {
        try
        {
            // Radio de 30km desde Córdoba capital
            var response = await _http.GetAsync("/incidents/near?lat=-31.4201&lon=-64.1888&radius=30000");
            response.EnsureSuccessStatusCode();
            var incidencias = await response.Content.ReadFromJsonAsync<List<IncidenciaDto>>();
            return incidencias ?? new List<IncidenciaDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener incidencias: {ex.Message}");
            return new List<IncidenciaDto>();
        }
    }

    // ======================
    // 🔎 BUSCAR INCIDENCIAS (CORREGIDO)
    // ======================
    public async Task<List<IncidenciaDto>> BuscarIncidenciasAsync(int? categoriaId = null, string? descripcion = null, DateTimeOffset? desde = null, DateTimeOffset? hasta = null)
    {
        try
        {
            var queryParams = new List<string>();

            if (categoriaId.HasValue)
                queryParams.Add($"categoriaId={categoriaId.Value}");

            if (!string.IsNullOrWhiteSpace(descripcion))
                queryParams.Add($"descripcion={Uri.EscapeDataString(descripcion)}");

            if (desde.HasValue)
                queryParams.Add($"desde={desde.Value:yyyy-MM-dd}");

            if (hasta.HasValue)
                queryParams.Add($"hasta={hasta.Value:yyyy-MM-dd}");

            var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            var url = $"/incidents/filter{queryString}";

            Console.WriteLine($"🔍 Buscando incidencias: {url}");

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<List<IncidenciaDto>>();
            Console.WriteLine($"✅ Encontradas {result?.Count ?? 0} incidencias");

            return result ?? new List<IncidenciaDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al buscar incidencias: {ex.Message}");
            return new List<IncidenciaDto>();
        }
    }

    // ======================
    // 🆕 CREAR INCIDENCIA
    // ======================
    public async Task<bool> CrearIncidenciaAsync(int categoriaId, string descripcion, double lat, double lon)
    {
        try
        {
            var nuevaIncidencia = new
            {
                CategoriaId = categoriaId,
                Descripcion = descripcion,
                Lat = lat,
                Lon = lon
            };

            var json = JsonSerializer.Serialize(nuevaIncidencia);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/incidents", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear incidencia: {ex.Message}");
            return false;
        }
    }
}