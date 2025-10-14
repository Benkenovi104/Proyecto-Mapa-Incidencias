using System.Collections.ObjectModel;

namespace IntegrarMapa;

public partial class BuscarPage : ContentPage
{
    private ObservableCollection<Incidencia> resultados = new();

    public BuscarPage()
    {
        InitializeComponent();
        listaResultados.ItemsSource = resultados;
    }

    // ?? Evento cuando cambia el tipo de filtro
    private void OnFiltroChanged(object sender, EventArgs e)
    {
        FiltroContainer.Content = null;

        switch (pickerFiltro.SelectedItem?.ToString())
        {
            case "Categoría":
                var pickerCategoria = new Picker { Title = "Tipo de incidencia" };
                pickerCategoria.Items.Add("Pozo en la calle");
                pickerCategoria.Items.Add("Choque automovilístico");
                pickerCategoria.Items.Add("Incendio");
                FiltroContainer.Content = pickerCategoria;
                break;

            case "Nombre":
                var entryNombre = new Entry { Placeholder = "Buscar por nombre..." };
                FiltroContainer.Content = entryNombre;
                break;

            case "Fecha":
                var datePicker = new DatePicker { Format = "dd/MM/yyyy" };
                FiltroContainer.Content = datePicker;
                break;
        }
    }

    // ?? Simulación de búsqueda
    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        // En una versión futura se conectará a la base de datos
        resultados.Clear();

        // Simulamos resultados con base en el filtro elegido
        string filtro = pickerFiltro.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrEmpty(filtro))
        {
            await DisplayAlert("Error", "Seleccioná un tipo de filtro antes de buscar.", "OK");
            return;
        }

        // Generamos datos dummy para mostrar cómo funciona
        resultados.Add(new Incidencia
        {
            Titulo = $"Incidencia de ejemplo ({filtro})",
            Coordenadas = "Lat: -34.6037, Lon: -58.3816"
        });

        lblResultadosTitulo.IsVisible = true;
        listaResultados.IsVisible = true;
    }

    // ?? Volver al mapa
    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
