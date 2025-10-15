using IntegrarMapa.Services;

namespace IntegrarMapa;

public partial class ChangePasswordPage : ContentPage
{
    private readonly ApiService _apiService = new();

    public ChangePasswordPage()
    {
        InitializeComponent();
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        string email = entryEmail.Text?.Trim() ?? string.Empty;
        string oldPass = entryOldPassword.Text?.Trim() ?? string.Empty;
        string newPass = entryNewPassword.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
        {
            await DisplayAlert("Error", "Debes completar todos los campos", "OK");
            return;
        }

        bool ok = await _apiService.ChangePasswordAsync(email, oldPass, newPass);

        if (ok)
            await DisplayAlert("Éxito", "Contraseña actualizada correctamente", "OK");
        else
            await DisplayAlert("Error", "No se pudo actualizar la contraseña. Verifica los datos.", "OK");
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
