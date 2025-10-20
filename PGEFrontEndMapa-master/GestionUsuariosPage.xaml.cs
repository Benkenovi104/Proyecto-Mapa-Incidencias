using IntegrarMapa.Models;
using IntegrarMapa.Services;

namespace IntegrarMapa;

public partial class GestionUsuariosPage : ContentPage
{
    private readonly ApiService _apiService = new();
    private UsuarioDto? _usuarioSeleccionado;
    private List<UsuarioDto>? _usuariosEncontrados;

    public GestionUsuariosPage()
    {
        InitializeComponent();
        CargarOpcionesBusqueda();
        OcultarSeccionUsuario();
        OcultarListaUsuarios();
    }

    private void CargarOpcionesBusqueda()
    {
        // Opciones de búsqueda
        pickerCriterio.ItemsSource = new List<string> { "Nombre", "Email", "Usuario" };

        // Opciones de roles (deben coincidir con los roles en tu BD)
        pickerRol.ItemsSource = new List<string> { "vecino", "administrador" };
    }

    private void MostrarSeccionUsuario()
    {
        seccionUsuario.IsVisible = true;
    }

    private void OcultarSeccionUsuario()
    {
        seccionUsuario.IsVisible = false;
    }

    private void MostrarListaUsuarios()
    {
        listaUsuarios.IsVisible = true;
    }

    private void OcultarListaUsuarios()
    {
        listaUsuarios.IsVisible = false;
    }

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        try
        {
            string criterio = pickerCriterio.SelectedItem?.ToString() ?? "";
            string valor = entryBusqueda.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(criterio) || string.IsNullOrEmpty(valor))
            {
                await DisplayAlert("Error", "Debes seleccionar un criterio y ingresar un valor", "OK");
                return;
            }

            // Mostrar loading
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;

            // Buscar usuarios en la API
            _usuariosEncontrados = await _apiService.BuscarUsuariosAsync(criterio, valor);

            if (_usuariosEncontrados != null && _usuariosEncontrados.Any())
            {
                if (_usuariosEncontrados.Count == 1)
                {
                    // Si solo hay un resultado, mostrarlo directamente
                    _usuarioSeleccionado = _usuariosEncontrados.First();
                    MostrarUsuario(_usuarioSeleccionado);
                    MostrarSeccionUsuario();
                    OcultarListaUsuarios();

                    await DisplayAlert("Éxito", $"Se encontró a {_usuarioSeleccionado.Nombre}", "OK");
                }
                else
                {
                    // Si hay múltiples resultados, mostrar la lista
                    collectionUsuarios.ItemsSource = _usuariosEncontrados;
                    MostrarListaUsuarios();
                    OcultarSeccionUsuario();

                    await DisplayAlert("Resultados", $"Se encontraron {_usuariosEncontrados.Count} usuarios. Selecciona uno de la lista.", "OK");
                }
            }
            else
            {
                LimpiarCampos();
                OcultarSeccionUsuario();
                OcultarListaUsuarios();
                await DisplayAlert("Sin resultados", "No se encontró ningún usuario con esos datos.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al buscar usuario: {ex.Message}", "OK");
            OcultarSeccionUsuario();
            OcultarListaUsuarios();
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }

    private void OnUsuarioSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is UsuarioDto usuario)
        {
            _usuarioSeleccionado = usuario;
            MostrarUsuario(_usuarioSeleccionado);
            OcultarListaUsuarios();
            MostrarSeccionUsuario();

            // Opcional: mostrar confirmación
            DisplayAlert("Usuario seleccionado", $"Has seleccionado a {usuario.Nombre}", "OK");
        }
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (_usuarioSeleccionado == null)
        {
            await DisplayAlert("Error", "No hay usuario seleccionado", "OK");
            return;
        }

        try
        {
            string nuevoRol = pickerRol.SelectedItem?.ToString() ?? "vecino";

            // Mostrar loading
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;

            bool exito = await _apiService.CambiarRolUsuarioAsync(_usuarioSeleccionado.Id, nuevoRol);

            if (exito)
            {
                _usuarioSeleccionado.Rol = nuevoRol;
                await DisplayAlert("Éxito", $"Se actualizó el rol de {_usuarioSeleccionado.Nombre} a {nuevoRol}", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar el rol", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al actualizar rol: {ex.Message}", "OK");
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        if (_usuarioSeleccionado == null)
        {
            await DisplayAlert("Error", "No hay usuario seleccionado", "OK");
            return;
        }

        bool confirmar = await DisplayAlert(
            "Eliminar Usuario",
            $"¿Estás seguro de eliminar al usuario {_usuarioSeleccionado.Nombre}?\n\nEsta acción no se puede deshacer.",
            "Sí, eliminar",
            "Cancelar");

        if (confirmar)
        {
            try
            {
                // Mostrar loading
                loadingIndicator.IsVisible = true;
                loadingIndicator.IsRunning = true;

                bool exito = await _apiService.EliminarUsuarioAsync(_usuarioSeleccionado.Id);

                if (exito)
                {
                    await DisplayAlert("Éxito", $"{_usuarioSeleccionado.Nombre} fue eliminado correctamente", "OK");
                    LimpiarCampos();
                    OcultarSeccionUsuario();
                    _usuarioSeleccionado = null;
                    entryBusqueda.Text = string.Empty;
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar el usuario", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar usuario: {ex.Message}", "OK");
            }
            finally
            {
                loadingIndicator.IsVisible = false;
                loadingIndicator.IsRunning = false;
            }
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private void MostrarUsuario(UsuarioDto usuario)
    {
        lblNombre.Text = usuario.Nombre;
        lblEmail.Text = usuario.Email;
        lblUsuario.Text = usuario.Username;
        pickerRol.SelectedItem = usuario.Rol;
    }

    private void LimpiarCampos()
    {
        lblNombre.Text = "-----";
        lblEmail.Text = "-----";
        lblUsuario.Text = "-----";
        pickerRol.SelectedIndex = -1;
    }

    private void OnCancelarSeleccionClicked(object sender, EventArgs e)
    {
        OcultarListaUsuarios();
        entryBusqueda.Text = string.Empty;
        _usuariosEncontrados = null;
    }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = "vecino";
}