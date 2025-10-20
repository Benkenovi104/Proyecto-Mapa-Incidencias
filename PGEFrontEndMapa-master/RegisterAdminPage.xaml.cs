using IntegrarMapa.Models;
using IntegrarMapa.Services;

namespace IntegrarMapa;

public partial class AgregarOperadorPage : ContentPage
{
    private readonly ApiService _apiService = new();

    public AgregarOperadorPage()
    {
        InitializeComponent();
    }

    private async void OnCrearOperadorClicked(object sender, EventArgs e)
    {
        string nombre = entryNombre.Text?.Trim() ?? string.Empty;
        string apellido = entryApellido.Text?.Trim() ?? string.Empty;
        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string username = entryUsername.Text?.Trim() ?? string.Empty;
        string password = entryPassword.Text?.Trim() ?? string.Empty;
        string rol = "administrador"; // rol fijo

        if (string.IsNullOrEmpty(nombre) ||
            string.IsNullOrEmpty(apellido) ||
            string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Debes completar todos los campos", "OK");
            return;
        }

        var success = await _apiService.RegisterWithRoleAsync(nombre, apellido, email, username, password, rol);

        if (success)
        {
            await DisplayAlert("Éxito", "Operador agregado correctamente", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Error", "No se pudo agregar el operador", "OK");
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
