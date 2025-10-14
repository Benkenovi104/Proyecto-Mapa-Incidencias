namespace IntegrarMapa;

public partial class PerfilPage : ContentPage
{
    public PerfilPage()
    {
        InitializeComponent();
    }

    private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Cambiar contraseña",
            "Aquí podrás cambiar tu contraseña (lógica pendiente de base de datos).",
            "OK");
    }

    private async void OnEliminarCuentaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Eliminar cuenta",
            "¿Estás seguro de que querés eliminar tu cuenta? Esta acción no se puede deshacer.",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            await DisplayAlert("Cuenta eliminada",
                "Tu cuenta fue eliminada correctamente (simulado).",
                "OK");
            await Navigation.PopModalAsync();
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
