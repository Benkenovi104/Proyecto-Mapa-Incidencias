using IntegrarMapa.Models;
using IntegrarMapa.Services;
using Microsoft.Maui.Controls;

namespace IntegrarMapa;

public partial class PanelIncidenciasPage : ContentPage
{
    private readonly ApiService _apiService = new();
    private List<IncidenciaDto> _todasIncidencias = new();
    private List<CategoriaDto> _categorias = new();
    private PaginationInfo _paginationInfo = new();
    private int _currentPage = 1;
    private const int _pageSize = 10;

    public PanelIncidenciasPage()
    {
        InitializeComponent();
        _ = InicializarFiltros();
    }

    private async Task InicializarFiltros()
    {
        await CargarOpcionesBusqueda();
        await CargarIncidenciasPrimeraPagina();
    }

    private async Task CargarOpcionesBusqueda()
    {
        try
        {
            // Cargar categorías
            _categorias = await _apiService.ObtenerCategoriasAsync();
            var opcionesCategorias = new List<string> { "Todas las categorías" };
            opcionesCategorias.AddRange(_categorias.Select(c => c.Nombre));
            FiltroCategoria.ItemsSource = opcionesCategorias;
            FiltroCategoria.SelectedIndex = 0;

            // Cargar estados
            var opcionesEstados = new List<string>
            {
                "Todos los estados",
                "nueva",
                "en_proceso",
                "resuelta",
                "cerrada"
            };
            FiltroEstado.ItemsSource = opcionesEstados;
            FiltroEstado.SelectedIndex = 0;

            // Fechas ya está configurado en XAML
            FiltroFecha.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar opciones de búsqueda: {ex.Message}");
            await DisplayAlert("Error", "No se pudieron cargar las opciones de filtro", "OK");
        }
    }

    private async Task CargarIncidenciasPrimeraPagina()
    {
        try
        {
            Console.WriteLine("🔄 Cargando primera página de incidencias...");
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;

            _currentPage = 1;
            var (incidencias, pagination) = await _apiService.ObtenerIncidenciasAsync(_currentPage, _pageSize);

            _todasIncidencias = incidencias;
            _paginationInfo = pagination;

            Console.WriteLine($"📊 {_todasIncidencias.Count} incidencias cargadas (Página {_currentPage})");

            if (_todasIncidencias.Any())
            {
                TablaIncidencias.ItemsSource = _todasIncidencias;
                ActualizarUIpaginacion();
            }
            else
            {
                LblResultado.Text = "No hay incidencias registradas";
                LblResultado.IsVisible = true;
                TablaIncidencias.ItemsSource = new List<IncidenciaDto>();
                BtnCargarMas.IsVisible = false;
                LblPaginacion.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error en CargarIncidenciasPrimeraPagina: {ex.Message}");
            await DisplayAlert("Error", $"Error al cargar incidencias: {ex.Message}", "OK");
            LblResultado.Text = "Error al cargar incidencias";
            LblResultado.IsVisible = true;
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }

    private async void OnCargarMasClicked(object sender, EventArgs e)
    {
        try
        {
            Console.WriteLine($"🔄 Cargando página {_currentPage + 1}...");
            loadingIndicator.IsVisible = true;
            BtnCargarMas.IsEnabled = false;

            _currentPage++;
            var (nuevasIncidencias, pagination) = await _apiService.ObtenerIncidenciasAsync(_currentPage, _pageSize);

            if (nuevasIncidencias.Any())
            {
                _todasIncidencias.AddRange(nuevasIncidencias);
                _paginationInfo = pagination;

                // Actualizar la lista (usando una nueva referencia para forzar refresh)
                TablaIncidencias.ItemsSource = null;
                TablaIncidencias.ItemsSource = _todasIncidencias;

                ActualizarUIpaginacion();
                Console.WriteLine($"✅ {nuevasIncidencias.Count} nuevas incidencias cargadas. Total: {_todasIncidencias.Count}");
            }
            else
            {
                _currentPage--; // Revertir si no hay más incidencias
                await DisplayAlert("Información", "No hay más incidencias para cargar", "OK");
            }
        }
        catch (Exception ex)
        {
            _currentPage--; // Revertir en caso de error
            Console.WriteLine($"💥 Error al cargar más incidencias: {ex.Message}");
            await DisplayAlert("Error", "No se pudieron cargar más incidencias", "OK");
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            BtnCargarMas.IsEnabled = true;
        }
    }

    private void ActualizarUIpaginacion()
    {
        LblPaginacion.Text = _paginationInfo.DisplayText;
        LblPaginacion.IsVisible = true;

        LblResultado.Text = $"Mostrando {_todasIncidencias.Count} de {_paginationInfo.TotalCount} incidencias";
        LblResultado.IsVisible = true;

        BtnCargarMas.IsVisible = _paginationInfo.HasNextPage;
        BtnCargarMas.Text = $"Cargar {_pageSize} incidencias más";
    }

    // Los demás métodos (OnEditarClicked, OnEliminarClicked, OnBuscarClicked, etc.)
    // se mantienen igual que en la versión anterior...

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is IncidenciaDto incidencia)
        {
            string nuevoEstado = await DisplayActionSheet(
                $"Cambiar estado de incidencia #{incidencia.Id}",
                "Cancelar", null,
                "nueva", "en_proceso", "resuelta", "cerrada");

            if (!string.IsNullOrEmpty(nuevoEstado) && nuevoEstado != "Cancelar")
            {
                try
                {
                    loadingIndicator.IsVisible = true;
                    loadingIndicator.IsRunning = true;

                    bool exito = await _apiService.ActualizarIncidenciaAsync(incidencia.Id, nuevoEstado);

                    if (exito)
                    {
                        incidencia.Estado = nuevoEstado;
                        RefrescarTabla();
                        await DisplayAlert("Éxito", $"Estado actualizado a {nuevoEstado}", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo actualizar el estado", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al actualizar: {ex.Message}", "OK");
                }
                finally
                {
                    loadingIndicator.IsVisible = false;
                    loadingIndicator.IsRunning = false;
                }
            }
        }
    }

    private async void OnEliminarClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is IncidenciaDto incidencia)
        {
            bool confirmar = await DisplayAlert(
                "Eliminar Incidencia",
                $"¿Eliminar incidencia #{incidencia.Id}?\n\nDescripción: {incidencia.Descripcion}",
                "Sí", "No");

            if (confirmar)
            {
                try
                {
                    loadingIndicator.IsVisible = true;
                    loadingIndicator.IsRunning = true;

                    bool exito = await _apiService.EliminarIncidenciaAsync(incidencia.Id);

                    if (exito)
                    {
                        _todasIncidencias.Remove(incidencia);
                        RefrescarTabla();
                        await DisplayAlert("Éxito", "Incidencia eliminada", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo eliminar la incidencia", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al eliminar: {ex.Message}", "OK");
                }
                finally
                {
                    loadingIndicator.IsVisible = false;
                    loadingIndicator.IsRunning = false;
                }
            }
        }
    }

    private void RefrescarTabla()
    {
        TablaIncidencias.ItemsSource = null;
        TablaIncidencias.ItemsSource = _todasIncidencias;

        if (ResultadosView.IsVisible)
        {
            OnBuscarClicked(null, EventArgs.Empty);
        }
    }

    private void OnBuscarClicked(object sender, EventArgs e)
    {
        // Para búsquedas, volvemos a cargar desde la primera página
        _ = CargarIncidenciasPrimeraPagina();
    }

    private void OnLimpiarFiltrosClicked(object sender, EventArgs e)
    {
        FiltroCategoria.SelectedIndex = 0;
        FiltroEstado.SelectedIndex = 0;
        FiltroUsuario.Text = string.Empty;
        FiltroFecha.SelectedIndex = 0;
        ResultadosView.IsVisible = false;

        // Recargar primera página al limpiar filtros
        _ = CargarIncidenciasPrimeraPagina();
    }

    private async void OnVolverClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnRefrescarClicked(object sender, EventArgs e)
    {
        await InicializarFiltros();
    }
}