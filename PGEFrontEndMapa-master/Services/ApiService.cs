using IntegrarMapa.Config;
using IntegrarMapa.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using IntegrarMapa.Helpers; 

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
        try
        {
            var dto = new ChangePasswordDto
            {
                Email = email,
                OldPassword = oldPassword,
                NewPassword = newPassword
            };

            Console.WriteLine($"🔐 Intentando cambiar contraseña para: {email}");

            var response = await _http.PostAsJsonAsync("/auth/change-password", dto);

            Console.WriteLine($"📡 Respuesta del servidor: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Contraseña cambiada exitosamente");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error del servidor: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error de conexión: {ex.Message}");
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
    // 📋 PANEL DE INCIDENCIAS - CON PAGINACIÓN
    // ======================

    public async Task<(List<IncidenciaDto> Incidencias, PaginationInfo Pagination)> ObtenerIncidenciasAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var response = await _http.GetAsync($"/incidents/all?page={page}&pageSize={pageSize}");
            Console.WriteLine($"🔍 Llamando a: /incidents/all?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Respuesta paginada recibida");

                // Deserializar la respuesta que ahora incluye paginación
                var result = await response.Content.ReadFromJsonAsync<PagedResponse<IncidenciaDto>>();

                if (result != null)
                {
                    Console.WriteLine($"✅ {result.Incidencias?.Count ?? 0} incidencias en página {result.Pagination?.CurrentPage ?? 1}");
                    return (result.Incidencias ?? new List<IncidenciaDto>(), result.Pagination ?? new PaginationInfo());
                }
            }
            else
            {
                Console.WriteLine($"❌ Error en /incidents/all: {response.StatusCode}");
            }

            return (new List<IncidenciaDto>(), new PaginationInfo());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción en ObtenerIncidenciasAsync: {ex.Message}");
            return (new List<IncidenciaDto>(), new PaginationInfo());
        }
    }

    public async Task<bool> ActualizarIncidenciaAsync(int id, string nuevoEstado)
    {
        try
        {
            // Convertir el estado de texto a ID numérico según tu tabla de estados
            int estadoId = nuevoEstado switch
            {
                "nueva" => 1,
                "en_proceso" => 2,
                "resuelta" => 3,
                "cerrada" => 4,
                _ => 1
            };

            // Enviar solo el estadoId como número, no como objeto
            var response = await _http.PatchAsJsonAsync($"/incidents/{id}/status", estadoId);

            Console.WriteLine($"📤 Actualizando incidencia {id} a estado_id: {estadoId}");
            Console.WriteLine($"📥 Respuesta: {response.StatusCode}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al actualizar incidencia: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EliminarIncidenciaAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/incidents/{id}");
            Console.WriteLine($"🗑️ Eliminando incidencia {id}, respuesta: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al eliminar incidencia: {ex.Message}");
            return false;
        }
    }


    // ======================
    // 🆕 Crear nuevo operador (administrador)
    // ======================
    public async Task<bool> RegisterWithRoleAsync(string nombre, string apellido, string email, string username, string password, string rol)
    {
        try
        {
            var data = new
            {
                Nombre = nombre,
                Apellido = apellido,
                Email = email,
                Username = username,
                Password = password,
                Rol = rol // "administrador"
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Usar el endpoint específico para administradores
            var response = await _http.PostAsync($"{ApiConfig.BaseUrl}/auth/register-admin", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear operador: {ex.Message}");
            return false;
        }
    }

    // ======================
    // 🆕 CREAR INCIDENCIA (CON FOTO)
    // ======================
    public async Task<bool> CrearIncidenciaAsync(int categoriaId, string descripcion, double lat, double lon, string fotoUrl)
    {
        try
        {
            // Usar SesionUsuario.UserId directamente
            var nuevaIncidencia = new
            {
                UserId = SesionUsuario.UserId, // ← Usa tu SesionUsuario actual
                CategoriaId = categoriaId,
                Descripcion = descripcion,
                Lat = lat,
                Lon = lon,
                FotoUrl = fotoUrl // ← AHORA ES OBLIGATORIO
            };

            var json = JsonSerializer.Serialize(nuevaIncidencia);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/incidents", content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Incidencia creada exitosamente");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error al crear incidencia: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción al crear incidencia: {ex.Message}");
            return false;
        }
    }


    // ======================
    // 👤 PERFIL DE USUARIO
    // ======================

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        try
        {
            var response = await _http.GetAsync($"/users/{userId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<UserProfileDto>();

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener perfil: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateUserProfileAsync(int userId, string campo, string valor)
    {
        try
        {
            var updateData = new Dictionary<string, string>();

            switch (campo.ToLower())
            {
                case "nombre":
                    updateData["Nombre"] = valor;
                    break;
                case "apellido":
                    updateData["Apellido"] = valor;
                    break;
                case "username":
                    updateData["Username"] = valor;
                    break;
                case "email":
                    updateData["Email"] = valor;
                    break;
            }

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"/users/{userId}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al actualizar perfil: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar usuario: {ex.Message}");
            return false;
        }
    }



    // ======================
    // 👥 GESTIÓN DE USUARIOS
    // ======================

    public async Task<List<UsuarioDto>?> BuscarUsuariosAsync(string criterio, string valor)
    {
        try
        {
            var response = await _http.GetAsync($"/users/search?criterio={criterio}&valor={Uri.EscapeDataString(valor)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<UsuarioDto>>();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al buscar usuarios: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CambiarRolUsuarioAsync(int userId, string nuevoRol)
    {
        try
        {
            var dto = new { Rol = nuevoRol };
            var response = await _http.PatchAsJsonAsync($"/users/{userId}/role", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cambiar rol: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EliminarUsuarioAsync(int userId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al eliminar usuario: {ex.Message}");
            return false;
        }
    }

    // ======================
    // 🗂️ MÉTODOS ADICIONALES PARA FILTROS
    // ======================

    public async Task<List<CategoriaDto>> ObtenerCategoriasAsync()
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
    // 📸 SUBIR FOTO
    // ======================
    public async Task<UploadPhotoResponse?> SubirFotoAsync(byte[] imageBytes, string fileName, string contentType)
    {
        try
        {
            var base64Content = Convert.ToBase64String(imageBytes);

            var dto = new UploadPhotoDto
            {
                FileName = fileName,
                ContentType = contentType,
                Base64Content = base64Content
            };

            var response = await _http.PostAsJsonAsync("/incidents/upload-photo", dto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UploadPhotoResponse>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error al subir foto: {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción al subir foto: {ex.Message}");
            return null;
        }
    }

}