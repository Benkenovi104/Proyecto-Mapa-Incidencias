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

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        try
        {
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;

            // ======== Obtener valores de los filtros ========

            int? categoriaId = null;
            if (FiltroCategoria.SelectedIndex > 0)
            {
                var categoriaSeleccionada = _categorias[FiltroCategoria.SelectedIndex - 1];
                categoriaId = categoriaSeleccionada.Id;
            }

            string? estado = null;
            if (FiltroEstado.SelectedIndex > 0)
            {
                estado = FiltroEstado.SelectedItem?.ToString();
            }

            string? descripcion = string.IsNullOrWhiteSpace(FiltroDescripcion.Text)
                ? null
                : FiltroDescripcion.Text.Trim();

            string? usuario = string.IsNullOrWhiteSpace(FiltroUsuario.Text)
                ? null
                : FiltroUsuario.Text.Trim();

            DateTimeOffset? desde = null;
            DateTimeOffset? hasta = null;

            if (FiltroFechaDesde?.Date != null && FiltroFechaDesde.Date != DateTime.MinValue)
                desde = FiltroFechaDesde.Date;

            if (FiltroFechaHasta?.Date != null && FiltroFechaHasta.Date != DateTime.MinValue)
                hasta = FiltroFechaHasta.Date;

            // ======== Llamar al ApiService con filtros ========
            // CORREGIDO: Usar solo los 4 parámetros que acepta tu ApiService
            List<IncidenciaDto> incidenciasFiltradas;

            try
            {
                // Solo pasar los parámetros que tu ApiService acepta
                incidenciasFiltradas = await _apiService.BuscarIncidenciasAsync(
                    categoriaId: categoriaId,
                    descripcion: descripcion,
                    desde: desde,
                    hasta: hasta
                );

                // Si necesitas filtrar por estado o usuario, hacerlo localmente
                if (!string.IsNullOrEmpty(estado) || !string.IsNullOrEmpty(usuario))
                {
                    incidenciasFiltradas = incidenciasFiltradas.Where(i =>
                        (string.IsNullOrEmpty(estado) || i.Estado?.Equals(estado, StringComparison.OrdinalIgnoreCase) == true) &&
                        (string.IsNullOrEmpty(usuario) || i.UsuarioNombre?.Contains(usuario, StringComparison.OrdinalIgnoreCase) == true)
                    ).ToList();
                }
            }
            catch (Exception apiEx)
            {
                Console.WriteLine($"❌ Error en búsqueda API: {apiEx.Message}");
                // Si falla la API, usar filtrado local completo
                incidenciasFiltradas = await FiltrarIncidenciasLocalmenteAsync(
                    categoriaId, descripcion, desde, hasta, estado, usuario);
            }

            // ======== Mostrar resultados ========
            if (incidenciasFiltradas != null && incidenciasFiltradas.Any())
            {
                ResultadosView.IsVisible = true;
                ResultadosCollection.ItemsSource = incidenciasFiltradas;
                LblResultado.Text = $"{incidenciasFiltradas.Count} resultados encontrados";
                LblResultado.IsVisible = true;
            }
            else
            {
                ResultadosView.IsVisible = false;
                LblResultado.Text = "No se encontraron incidencias con esos filtros";
                LblResultado.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un error al buscar incidencias: {ex.Message}", "OK");
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }


    // Método alternativo si tu ApiService no tiene búsqueda
    private async Task<List<IncidenciaDto>> FiltrarIncidenciasLocalmenteAsync(
        int? categoriaId, string? descripcion, DateTimeOffset? desde,
        DateTimeOffset? hasta, string? estado, string? usuario)
    {
        // Cargar todas las incidencias primero
        var todasIncidencias = new List<IncidenciaDto>();
        int pagina = 1;

        while (true)
        {
            var (incidencias, pagination) = await _apiService.ObtenerIncidenciasAsync(pagina, 50);
            if (!incidencias.Any()) break;

            todasIncidencias.AddRange(incidencias);
            if (!pagination.HasNextPage) break;
            pagina++;
        }

        // Aplicar filtros localmente
        var query = todasIncidencias.AsEnumerable();

        if (categoriaId.HasValue)
            query = query.Where(i => i.CategoriaId == categoriaId.Value);

        if (!string.IsNullOrEmpty(descripcion))
            query = query.Where(i => i.Descripcion?.Contains(descripcion, StringComparison.OrdinalIgnoreCase) == true);

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(i => i.Estado?.Equals(estado, StringComparison.OrdinalIgnoreCase) == true);

        if (!string.IsNullOrEmpty(usuario))
            query = query.Where(i => i.UsuarioNombre?.Contains(usuario, StringComparison.OrdinalIgnoreCase) == true);

        if (desde.HasValue)
            query = query.Where(i => i.Fecha >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(i => i.Fecha <= hasta.Value);

        return query.ToList();
    }

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
            OnBuscarClicked(this, EventArgs.Empty);
        }
    }

    private void OnLimpiarFiltrosClicked(object sender, EventArgs e)
    {
        FiltroCategoria.SelectedIndex = 0;
        FiltroEstado.SelectedIndex = 0;
        FiltroUsuario.Text = string.Empty;
        FiltroDescripcion.Text = string.Empty;

        // Limpiar fechas
        if (FiltroFechaDesde != null)
            FiltroFechaDesde.Date = DateTime.Now;
        if (FiltroFechaHasta != null)
            FiltroFechaHasta.Date = DateTime.Now;

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