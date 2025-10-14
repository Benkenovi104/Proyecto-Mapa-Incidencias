namespace IntegrarMapa;

public partial class PerfilPage : ContentPage
{
    public PerfilPage()
    {
        InitializeComponent();
    }

    // 🔙 Volver
    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    // 🔒 Cambiar contraseña
    private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Cambiar contraseña",
            "Aquí podrás cambiar tu contraseña (pendiente conexión con base de datos).",
            "OK");
    }

    // 🗑️ Eliminar cuenta
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

    // ✏️ Editar campos
    private async void OnEditarNombreClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync("Editar nombre", "Ingresá tu nombre:", initialValue: lblNombre.Text);
        if (!string.IsNullOrWhiteSpace(nuevo))
            lblNombre.Text = nuevo;
    }

    private async void OnEditarApellidoClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync("Editar apellido", "Ingresá tu apellido:", initialValue: lblApellido.Text);
        if (!string.IsNullOrWhiteSpace(nuevo))
            lblApellido.Text = nuevo;
    }

    private async void OnEditarUsuarioClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync("Editar usuario", "Ingresá tu nombre de usuario:", initialValue: lblUsuario.Text);
        if (!string.IsNullOrWhiteSpace(nuevo))
            lblUsuario.Text = nuevo;
    }

    private async void OnEditarEmailClicked(object sender, EventArgs e)
    {
        string nuevo = await DisplayPromptAsync("Editar email", "Ingresá tu correo electrónico:", initialValue: lblEmail.Text, keyboard: Keyboard.Email);
        if (!string.IsNullOrWhiteSpace(nuevo))
            lblEmail.Text = nuevo;
    }
}
