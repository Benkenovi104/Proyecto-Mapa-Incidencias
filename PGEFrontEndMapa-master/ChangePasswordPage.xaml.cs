using IntegrarMapa.Services;

namespace IntegrarMapa;

public partial class ChangePasswordPage : ContentPage
{
    private readonly ApiService _apiService = new();
    private readonly bool _esDesdePerfil;

    // Constructor para usar desde el perfil (con email pre-cargado)
    public ChangePasswordPage(string userEmail)
    {
        InitializeComponent();
        entryEmail.Text = userEmail;
        entryEmail.IsEnabled = false; // No permitir editar el email si viene del perfil
        _esDesdePerfil = true;

        // Cambiar título para indicar que viene del perfil
        Title = "Cambiar Mi Contraseña";
    }

    // Constructor para usar desde el login (sin email pre-cargado)
    public ChangePasswordPage()
    {
        InitializeComponent();
        entryEmail.IsEnabled = true; // Permitir editar el email si viene del login
        _esDesdePerfil = false;
        Title = "Recuperar Contraseña";
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        try
        {
            string email = entryEmail.Text?.Trim() ?? string.Empty;
            string oldPass = entryContrasenaActual.Text?.Trim() ?? string.Empty;
            string newPass = entryNuevaContrasena.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                await DisplayAlert("Error", "Debes completar todos los campos", "OK");
                return;
            }

            if (newPass.Length < 6)
            {
                await DisplayAlert("Error", "La nueva contraseña debe tener al menos 6 caracteres", "OK");
                return;
            }

            // Mostrar indicador de carga
            CambiarEstadoControles(false);

            bool ok = await _apiService.ChangePasswordAsync(email, oldPass, newPass);

            // Restaurar controles
            CambiarEstadoControles(true);

            if (ok)
            {
                await DisplayAlert("Éxito", "Contraseña actualizada correctamente", "OK");

                // Limpiar campos
                entryContrasenaActual.Text = string.Empty;
                entryNuevaContrasena.Text = string.Empty;

                // Cerrar automáticamente solo si viene del perfil
                if (_esDesdePerfil)
                {
                    await CerrarPaginaSeguro();
                }
                // Si viene del login, dejar que el usuario cierre manualmente
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar la contraseña. Verifica la contraseña actual.", "OK");
            }
        }
        catch (Exception ex)
        {
            CambiarEstadoControles(true);
            await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await CerrarPaginaSeguro();
    }

    private async Task CerrarPaginaSeguro()
    {
        try
        {
            if (_esDesdePerfil)
            {
                // Si viene del perfil (modal), usar PopModalAsync
                if (Navigation.ModalStack.Count > 0)
                {
                    await Navigation.PopModalAsync();
                }
                else
                {
                    // Fallback seguro
                    await Navigation.PopAsync();
                }
            }
            else
            {
                // Si viene del login (navegación normal), usar PopAsync
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cerrar página: {ex.Message}");
            // Si hay error, intentar cualquier método disponible
            if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
        }
    }

    private void CambiarEstadoControles(bool habilitado)
    {
        entryEmail.IsEnabled = habilitado && !_esDesdePerfil; // Solo habilitar email si no viene del perfil
        entryContrasenaActual.IsEnabled = habilitado;
        entryNuevaContrasena.IsEnabled = habilitado;
    }

    // Manejar el botón físico de retroceso en Android
    protected override bool OnBackButtonPressed()
    {
        _ = CerrarPaginaSeguro();
        return true; // Indicar que manejamos el evento
    }
}