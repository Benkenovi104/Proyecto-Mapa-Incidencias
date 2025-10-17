using IntegrarMapa.Models;
using IntegrarMapa.Services;
using IntegrarMapa.Helpers;

namespace IntegrarMapa;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnIniciarSesionClicked(object sender, EventArgs e)
    {
        string email = entryUsuario.Text?.Trim() ?? string.Empty;
        string password = entryContrasena.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Debes completar todos los campos", "OK");
            return;
        }

        var result = await _apiService.LoginAsync(email, password);

        if (result == null)
        {
            await DisplayAlert("Error", "Usuario o contraseña incorrectos", "OK");
            return;
        }

        // 🧠 Guardar datos de sesión del usuario
        SesionUsuario.IniciarSesion(result.Id);

        await DisplayAlert("Bienvenido", $"Hola {result.Nombre} ({result.Rol})", "OK");

        // Redirigir según el rol
        if (Application.Current is App app)
        {
            if (result.Rol.ToLower() == "operador")
                app.SetMainPage(new MainPageOperador());
            else
                app.SetMainPage(new MainPage());
        }
    }

    private async void OnRegistrarseClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ChangePasswordPage());
    }
}
