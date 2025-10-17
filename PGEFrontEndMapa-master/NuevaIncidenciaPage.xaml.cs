using IntegrarMapa.Helpers;
using IntegrarMapa.Models;
using System.Net.Http.Json;

namespace IntegrarMapa;

public partial class NuevaIncidenciaPage : ContentPage
{
    private readonly double _lat;
    private readonly double _lon;
    private List<CategoriaDto>? _categorias;

    public NuevaIncidenciaPage(double lat, double lon)
    {
        InitializeComponent();
        _lat = lat;
        _lon = lon;

        lblCoordenadas.Text = $"📍 Lat: {_lat:F5}, Lon: {_lon:F5}";

        _ = CargarCategoriasAsync();
    }

    private async Task CargarCategoriasAsync()
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5102") };
            _categorias = await http.GetFromJsonAsync<List<CategoriaDto>>("/categories");

            if (_categorias != null)
            {
                foreach (var c in _categorias)
                    pickerCategoria.Items.Add(c.Nombre);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar las categorías:\n{ex.Message}", "OK");
        }
    }

    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        if (pickerCategoria.SelectedIndex < 0)
        {
            await DisplayAlert("Error", "Debe seleccionar una categoría", "OK");
            return;
        }

        try
        {
            var categoria = _categorias![pickerCategoria.SelectedIndex];

            var dto = new CreateIncidentDto
            {
                UserId = SesionUsuario.UserId,
                CategoriaId = categoria.Id,
                Descripcion = entryDescripcion.Text,
                FotoUrl = entryFoto.Text,
                Lat = _lat,
                Lon = _lon
            };

            using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5102") };
            var response = await http.PostAsJsonAsync("/incidents", dto);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Incidencia registrada correctamente", "OK");
                await Navigation.PopModalAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo registrar la incidencia:\n{error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al enviar la incidencia:\n{ex.Message}", "OK");
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
