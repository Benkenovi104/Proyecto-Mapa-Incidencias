namespace IntegrarMapa;

public partial class PerfilPage : ContentPage
{
    public PerfilPage()
    {
        InitializeComponent();
    }

    private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Cambiar contrase�a",
            "Aqu� podr�s cambiar tu contrase�a (l�gica pendiente de base de datos).",
            "OK");
    }

    private async void OnEliminarCuentaClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Eliminar cuenta",
            "�Est�s seguro de que quer�s eliminar tu cuenta? Esta acci�n no se puede deshacer.",
            "S�, eliminar",
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
