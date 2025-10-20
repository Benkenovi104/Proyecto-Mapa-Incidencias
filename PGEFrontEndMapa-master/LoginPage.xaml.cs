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
        // Ocultar mensajes previos
        lblMensajeError.IsVisible = false;
        lblMensajeExito.IsVisible = false;

        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string password = entryContrasena.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            MostrarError("Debes completar todos los campos");
            return;
        }

        var result = await _apiService.LoginAsync(email, password);

        if (result == null)
        {
            MostrarError("Mail o contraseña incorrectos");
            return;
        }

        // --- El inicio de sesión fue exitoso ---

        // 1. Mostrar mensaje de éxito en la UI
        MostrarExito($"¡Bienvenido, {result.Nombre}!");

        // 2. Esperar un momento para que el usuario vea el mensaje
        await Task.Delay(1500); // Espera 1.5 segundos

        // 3. Guardar datos de sesión del usuario
        SesionUsuario.IniciarSesion(result.Id);

        // 4. Redirigir según el rol
        if (Application.Current is App app)
        {
            if (result.Rol.ToLower() == "administrador")
                app.SetMainPage(new MainPageOperador());
            else
                app.SetMainPage(new MainPage());
        }
    }

    /// <summary>
    /// Método para mostrar un mensaje de error en la UI.
    /// </summary>
    private void MostrarError(string mensaje)
    {
        lblMensajeError.Text = mensaje;
        lblMensajeError.IsVisible = true;
    }

    /// <summary>
    /// Método para mostrar un mensaje de éxito en la UI.
    /// </summary>
    private void MostrarExito(string mensaje)
    {
        lblMensajeExito.Text = mensaje;
        lblMensajeExito.IsVisible = true;
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

