using IntegrarMapa.Helpers;
using IntegrarMapa.Models;
using IntegrarMapa.Services;
using System.ComponentModel.Design.Serialization;

namespace IntegrarMapa;

public partial class PerfilPage : ContentPage
{
    private readonly ApiService _apiService = new();
    private int _userId;

    public PerfilPage()
    {
        InitializeComponent();
        _userId = SesionUsuario.UserId;
        CargarDatosUsuario();
    }

    private async void CargarDatosUsuario()
    {
        try
        {
            // Mostrar loading
            loadingIndicator.IsVisible = true;
            contentStack.IsVisible = false;

            var userData = await _apiService.GetUserProfileAsync(_userId);

            if (userData != null)
            {
                lblNombre.Text = userData.Nombre ?? "No especificado";
                lblApellido.Text = userData.Apellido ?? "No especificado";
                lblUsuario.Text = userData.Username ?? "No especificado";
                lblEmail.Text = userData.Email ?? "No especificado";
            }
            else
            {
                await DisplayAlert("Error", "No se pudieron cargar los datos del usuario", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            contentStack.IsVisible = true;
        }
    }

    // 🔙 Volver
    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    // 🔒 Cambiar contraseña - Redirige al ChangePasswordPage existente
    private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
    {
        try
        {
            // Obtener el email actual del usuario
            string userEmail = lblEmail.Text;

            if (string.IsNullOrWhiteSpace(userEmail) || userEmail == "-----")
            {
                await DisplayAlert("Error", "No se pudo obtener el email del usuario", "OK");
                return;
            }

            // Usar PushModalAsync en lugar de PushAsync
            await Navigation.PushModalAsync(new ChangePasswordPage(userEmail));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir cambio de contraseña: {ex.Message}", "OK");
        }
    }

    // 🗑️ Eliminar cuenta
    private async void OnEliminarCuentaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Eliminar cuenta",
            "¿Estás seguro de que querés eliminar tu cuenta? Esta acción no se puede deshacer.",
            "Sí, eliminar",
            "Cancelar"
        );

        if (confirm)
        {
            try
            {
                bool eliminado = await _apiService.DeleteUserAsync(_userId);

                if (eliminado)
                {
                    await DisplayAlert(
                        "Cuenta eliminada",
                        "Tu cuenta fue eliminada correctamente.",
                        "OK"
                    );

                    // Cerrar sesión y redirigir al login
                    SesionUsuario.CerrarSesion();

                    if (Application.Current is App app)
                    {
                        app.SetMainPage(new LoginPage());
                    }
                }
                else
                {
                    await DisplayAlert(
                        "Error",
                        "No se pudo eliminar la cuenta. Inténtalo nuevamente.",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar cuenta: {ex.Message}", "OK");
            }
        }
    }

    // ✏️ Editar campos
    private async void OnEditarNombreClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync(
            "Editar nombre",
            "Ingresá tu nombre:",
            initialValue: lblNombre.Text
        );

        if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != lblNombre.Text)
        {
            await ActualizarCampo("nombre", nuevo);
        }
    }

    private async void OnEditarApellidoClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync(
            "Editar apellido",
            "Ingresá tu apellido:",
            initialValue: lblApellido.Text
        );

        if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != lblApellido.Text)
        {
            await ActualizarCampo("apellido", nuevo);
        }
    }

    private async void OnEditarUsuarioClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync(
            "Editar usuario",
            "Ingresá tu nombre de usuario:",
            initialValue: lblUsuario.Text
        );

        if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != lblUsuario.Text)
        {
            await ActualizarCampo("username", nuevo);
        }
    }

    private async void OnEditarEmailClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync(
            "Editar email",
            "Ingresá tu correo electrónico:",
            initialValue: lblEmail.Text,
            keyboard: Keyboard.Email
        );

        if (!string.IsNullOrWhiteSpace(nuevo) && nuevo != lblEmail.Text)
        {
            await ActualizarCampo("email", nuevo);
        }
    }

    private async Task ActualizarCampo(string campo, string nuevoValor)
    {
        try
        {
            bool actualizado = await _apiService.UpdateUserProfileAsync(_userId, campo, nuevoValor);

            if (actualizado)
            {
                // Actualizar la UI localmente
                switch (campo)
                {
                    case "nombre":
                        lblNombre.Text = nuevoValor;
                        break;
                    case "apellido":
                        lblApellido.Text = nuevoValor;
                        break;
                    case "username":
                        lblUsuario.Text = nuevoValor;
                        break;
                    case "email":
                        lblEmail.Text = nuevoValor;
                        break;
                }

                await DisplayAlert("Éxito", "Datos actualizados correctamente", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar el campo", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al actualizar: {ex.Message}", "OK");
        }
    }
}