namespace IntegrarMapa;

public partial class GestionUsuariosPage : ContentPage
{
    private List<Usuario> usuarios = new();

    public GestionUsuariosPage()
    {
        InitializeComponent();
        CargarUsuariosEjemplo();
    }

    private void CargarUsuariosEjemplo()
    {
        // Datos simulados
        usuarios = new List<Usuario>
        {
            new() { Nombre="Juan Perez", Email="juan@gmail.com", Telefono="3511234567", Username="juanp", Rol="Cliente" },
            new() { Nombre="Maria Lopez", Email="maria@gmail.com", Telefono="3519999999", Username="marial", Rol="Operador" }
        };

        // Opciones de búsqueda
        pickerCriterio.ItemsSource = new List<string> { "Nombre", "Email", "Usuario" };
    }

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        string criterio = pickerCriterio.SelectedItem?.ToString() ?? "";
        string valor = entryBusqueda.Text?.Trim().ToLower() ?? "";

        var encontrado = usuarios.FirstOrDefault(u =>
            (criterio == "Nombre" && u.Nombre.ToLower().Contains(valor)) ||
            (criterio == "Email" && u.Email.ToLower().Contains(valor)) ||
            (criterio == "Usuario" && u.Username.ToLower().Contains(valor))
        );

        if (encontrado != null)
        {
            lblNombre.Text = encontrado.Nombre;
            lblEmail.Text = encontrado.Email;
            lblTelefono.Text = encontrado.Telefono;
            lblUsuario.Text = encontrado.Username;
            pickerRol.SelectedItem = encontrado.Rol;

            await DisplayAlert("Usuario encontrado", $"Se encontró a {encontrado.Nombre}", "OK");
        }
        else
        {
            await DisplayAlert("Sin resultados", "No se encontró ningún usuario con esos datos.", "OK");
        }
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        string nuevoRol = pickerRol.SelectedItem?.ToString() ?? "Cliente";
        string usuario = lblUsuario.Text;

        var u = usuarios.FirstOrDefault(x => x.Username == usuario);
        if (u != null)
        {
            u.Rol = nuevoRol;
            await DisplayAlert("Éxito", $"Se actualizó el rol de {u.Nombre} a {nuevoRol}", "OK");
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        string usuario = lblUsuario.Text;
        var u = usuarios.FirstOrDefault(x => x.Username == usuario);

        if (u != null)
        {
            bool confirmar = await DisplayAlert("Eliminar", $"¿Eliminar al usuario {u.Nombre}?", "Sí", "No");
            if (confirmar)
            {
                usuarios.Remove(u);
                lblNombre.Text = lblEmail.Text = lblTelefono.Text = lblUsuario.Text = "-----";
                pickerRol.SelectedIndex = -1;
                await DisplayAlert("Eliminado", $"{u.Nombre} fue eliminado correctamente", "OK");
            }
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}

public class Usuario
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = "Cliente";
}
