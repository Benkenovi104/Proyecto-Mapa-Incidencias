namespace IntegrarMapa;

public partial class PanelIncidenciasPage : ContentPage
{
    private List<IncidenciaItem> todasIncidencias = new();

    public PanelIncidenciasPage()
    {
        InitializeComponent();
        CargarIncidencias();
    }

    private void CargarIncidencias()
    {
        // Datos de ejemplo
        todasIncidencias = new List<IncidenciaItem>
        {
            new() { Fecha = "1/1/12", Usuario = "Juan", Estado = "Aprobado" },
            new() { Fecha = "1/1/12", Usuario = "Pedro", Estado = "Pendiente" },
            new() { Fecha = "1/1/12", Usuario = "Matias", Estado = "Denegado" },
        };

        TablaIncidencias.ItemsSource = todasIncidencias;
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is IncidenciaItem item)
        {
            string nuevoEstado = await DisplayActionSheet(
                $"Cambiar estado de {item.Usuario}",
                "Cancelar", null,
                "Aprobado", "Pendiente", "Denegado");

            if (!string.IsNullOrEmpty(nuevoEstado))
            {
                item.Estado = nuevoEstado;
                RefrescarTabla();
            }
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is IncidenciaItem item)
        {
            bool confirmar = await DisplayAlert("Eliminar", $"¿Eliminar incidencia de {item.Usuario}?", "Sí", "No");
            if (confirmar)
            {
                todasIncidencias.Remove(item);
                RefrescarTabla();
            }
        }
    }

    private void RefrescarTabla()
    {
        TablaIncidencias.ItemsSource = null;
        TablaIncidencias.ItemsSource = todasIncidencias;
        ResultadosView.ItemsSource = null;
        ResultadosView.ItemsSource = todasIncidencias;
    }

    private void OnBuscarClicked(object sender, EventArgs e)
    {
        string usuario = FiltroUsuario.Text?.ToLower() ?? "";
        string estado = FiltroEstado.SelectedItem?.ToString() ?? "Todos";

        var filtradas = todasIncidencias.Where(i =>
            (string.IsNullOrEmpty(usuario) || i.Usuario.ToLower().Contains(usuario)) &&
            (estado == "Todos" || i.Estado == estado)
        ).ToList();

        if (filtradas.Any())
        {
            LblResultado.Text = "Cliente encontrado";
            LblResultado.IsVisible = true;
            ResultadosView.IsVisible = true;
            ResultadosView.ItemsSource = filtradas;
        }
        else
        {
            LblResultado.Text = "No se encontraron coincidencias";
            LblResultado.IsVisible = true;
            ResultadosView.IsVisible = false;
        }
    }

    private async void OnVolverClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }


    public class IncidenciaItem
    {
        public string Fecha { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
