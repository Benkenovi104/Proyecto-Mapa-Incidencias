using IntegrarMapa.Models;
using IntegrarMapa.Services;

namespace IntegrarMapa;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService = new();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnCrearCuentaClicked(object sender, EventArgs e)
    {
        string nombre = entryNombre.Text?.Trim() ?? string.Empty;
        string apellido = entryApellido.Text?.Trim() ?? string.Empty;
        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string username = entryUsername.Text?.Trim() ?? string.Empty;
        string password = entryPassword.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(nombre) ||
            string.IsNullOrEmpty(apellido) ||
            string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Debes completar todos los campos", "OK");
            return;
        }

        var success = await _apiService.RegisterAsync(nombre, apellido, email, username, password);

        if (success)
        {
            await DisplayAlert("Éxito", "Registro completado correctamente", "OK");
            await Navigation.PopAsync(); // Volver al login
        }
        else
        {
            await DisplayAlert("Error", "No se pudo registrar el usuario", "OK");
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
