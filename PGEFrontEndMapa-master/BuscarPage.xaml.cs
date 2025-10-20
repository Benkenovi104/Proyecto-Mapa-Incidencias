using IntegrarMapa.Models;
using IntegrarMapa.Services;
using System.Collections.ObjectModel;

namespace IntegrarMapa;

public partial class BuscarPage : ContentPage
{
    private readonly ApiService apiService = new();
    private ObservableCollection<IncidenciaDto> resultados = new();
    private List<CategoriaDto> categorias = new();

    public BuscarPage()
    {
        InitializeComponent();
        listaResultados.ItemsSource = resultados;
        _ = CargarCategoriasAsync();
    }

    private async Task CargarCategoriasAsync()
    {
        try
        {
            categorias = await apiService.GetCategoriasAsync();
            Console.WriteLine($"✅ Cargadas {categorias.Count} categorías");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar las categorías: {ex.Message}", "OK");
        }
    }

    private void OnFiltroChanged(object sender, EventArgs e)
    {
        FiltroContainer.Content = null;

        switch (pickerFiltro.SelectedItem?.ToString())
        {
            case "Categoría":
                var pickerCategoria = new Picker
                {
                    Title = "Seleccioná una categoría",
                    BackgroundColor = Color.FromArgb("#222"),
                    TextColor = Colors.White
                };

                foreach (var categoria in categorias)
                {
                    pickerCategoria.Items.Add(categoria.Nombre);
                }
                FiltroContainer.Content = pickerCategoria;
                break;

            case "Descripción":
                var entryDescripcion = new Entry
                {
                    Placeholder = "Buscar por descripción...",
                    BackgroundColor = Color.FromArgb("#222"),
                    TextColor = Colors.White,
                    PlaceholderColor = Colors.Gray
                };
                FiltroContainer.Content = entryDescripcion;
                break;

            case "Fecha":
                var stackFecha = new VerticalStackLayout { Spacing = 10 };

                var labelDesde = new Label
                {
                    Text = "Desde:",
                    TextColor = Colors.White,
                    FontSize = 14
                };

                var datePickerDesde = new DatePicker
                {
                    Format = "dd/MM/yyyy",
                    BackgroundColor = Color.FromArgb("#222"),
                    TextColor = Colors.White
                };

                var labelHasta = new Label
                {
                    Text = "Hasta:",
                    TextColor = Colors.White,
                    FontSize = 14
                };

                var datePickerHasta = new DatePicker
                {
                    Format = "dd/MM/yyyy",
                    BackgroundColor = Color.FromArgb("#222"),
                    TextColor = Colors.White
                };

                stackFecha.Children.Add(labelDesde);
                stackFecha.Children.Add(datePickerDesde);
                stackFecha.Children.Add(labelHasta);
                stackFecha.Children.Add(datePickerHasta);

                FiltroContainer.Content = stackFecha;
                break;
        }
    }

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        if (pickerFiltro.SelectedItem == null)
        {
            await DisplayAlert("Error", "Seleccioná un tipo de filtro.", "OK");
            return;
        }

        resultados.Clear();
        string filtro = pickerFiltro.SelectedItem.ToString()!;

        try
        {
            List<IncidenciaDto> incidenciasFiltradas = new();

            if (filtro == "Categoría" && FiltroContainer.Content is Picker pickerCategoria && pickerCategoria.SelectedIndex >= 0)
            {
                string categoriaNombre = pickerCategoria.SelectedItem.ToString()!;
                var categoria = categorias.FirstOrDefault(c => c.Nombre == categoriaNombre);

                if (categoria != null)
                {
                    Console.WriteLine($"🔍 Buscando por categoría: {categoria.Nombre} (ID: {categoria.Id})");
                    incidenciasFiltradas = await apiService.BuscarIncidenciasAsync(categoriaId: categoria.Id);
                }
            }
            else if (filtro == "Descripción" && FiltroContainer.Content is Entry entryDescripcion && !string.IsNullOrWhiteSpace(entryDescripcion.Text))
            {
                string texto = entryDescripcion.Text.Trim();
                Console.WriteLine($"🔍 Buscando por descripción: {texto}");
                incidenciasFiltradas = await apiService.BuscarIncidenciasAsync(descripcion: texto);
            }
            else if (filtro == "Fecha" && FiltroContainer.Content is VerticalStackLayout stackFecha)
            {
                var datePickerDesde = stackFecha.Children[1] as DatePicker;
                var datePickerHasta = stackFecha.Children[3] as DatePicker;

                if (datePickerDesde != null && datePickerHasta != null)
                {
                    var desde = new DateTimeOffset(datePickerDesde.Date);
                    var hasta = new DateTimeOffset(datePickerHasta.Date.AddDays(1).AddSeconds(-1)); // Fin del día

                    Console.WriteLine($"🔍 Buscando por fecha: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}");
                    incidenciasFiltradas = await apiService.BuscarIncidenciasAsync(desde: desde, hasta: hasta);
                }
            }

            foreach (var item in incidenciasFiltradas)
                resultados.Add(item);

            lblResultadosTitulo.IsVisible = resultados.Any();
            listaResultados.IsVisible = resultados.Any();

            if (!resultados.Any())
                await DisplayAlert("Sin resultados", "No se encontraron incidencias con ese criterio.", "OK");
            else
                await DisplayAlert("Éxito", $"Se encontraron {resultados.Count} incidencias", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron obtener las incidencias: {ex.Message}", "OK");
        }
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        try
        {
            // ✅ Usar PopModalAsync si esta página fue abierta modalmente
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al volver: {ex.Message}");
            // Fallback: ir directamente a MainPage
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}