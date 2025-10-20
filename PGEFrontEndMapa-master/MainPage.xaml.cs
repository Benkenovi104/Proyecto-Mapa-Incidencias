using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using IntegrarMapa.Models;
using IntegrarMapa.Services;
using IntegrarMapa.Helpers;


namespace IntegrarMapa;

public partial class MainPage : ContentPage
{
    private bool modoAgregar = false;
    private MemoryLayer pinLayer;
    private ObservableCollection<Incidencia> incidencias;
    private int tipoUsuario; // 1 = cliente, 2 = operador
    private readonly ApiService _apiService = new(); // ✅ Agregado


    public MainPage(int tipoUsuario = 2)
    {
        InitializeComponent();
        this.tipoUsuario = tipoUsuario;

        incidencias = new ObservableCollection<Incidencia>();

        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Capa de pines
        pinLayer = new MemoryLayer
        {
            Name = "Incidencias",
            Features = new List<IFeature>(),
            IsMapInfoLayer = true
        };
        map.Layers.Add(pinLayer);

        // Vista inicial → Cordoba
        var (x, y) = SphericalMercator.FromLonLat(-64.1888, -31.4201);
        var center = new MPoint(x, y);
        map.Home = n => n.CenterOn(center);

        mapControl.Map = map;

        // ✅ CAMBIO: Usar OnMapTapped en lugar de OnMapInfo
        mapControl.Info += OnMapTapped;

        // Cargar el menú inicial
        MenuContainer.Content = CrearVistaMenu();
    }

    // ✅ Se ejecuta cuando la página aparece
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarIncidenciasAsync(); // ← carga las incidencias en el mapa
    }

    // ✅ NUEVO MÉTODO: Manejar clicks en el mapa (íconos y mapa vacío)
    private async void OnMapTapped(object? sender, MapInfoEventArgs e)
    {
        // ✅ 1. Si se tocó un ícono/feature
        if (e.MapInfo?.Feature is PointFeature pointFeature)
        {
            // Obtener los datos de la incidencia desde el feature
            var descripcion = pointFeature["Descripcion"]?.ToString() ?? "Sin descripción";
            var categoriaId = pointFeature["CategoriaId"]?.ToString() ?? "0";
            var estado = pointFeature["Estado"]?.ToString() ?? "Desconocido";
            var iconoUrl = pointFeature["IconoUrl"]?.ToString();

            // Obtener coordenadas
            var point = pointFeature.Point;
            var (lon, lat) = SphericalMercator.ToLonLat(point.X, point.Y);

            Console.WriteLine($"📍 Ícono tocado: {descripcion} en {lat}, {lon}");

            // Mostrar detalles
            await MostrarDetalleIncidenciaAsync(descripcion, estado, lat, lon, iconoUrl);
        }
        // ✅ 2. Si está en modo agregar y se tocó el mapa vacío
        else if (modoAgregar && e.MapInfo?.WorldPosition is MPoint pos)
        {
            modoAgregar = false;
            await MostrarFormularioIncidenciaAsync(pos);
        }
    }

    // ✅ NUEVO MÉTODO: Mostrar detalles de incidencia al tocar ícono (usando DisplayAlert)
    private async Task MostrarDetalleIncidenciaAsync(string descripcion, string estado, double lat, double lon, string? iconoUrl)
    {
        // Crear mensaje con los detalles
        var mensaje = $"📝 {descripcion}\n\n" +
                     $"🔄 Estado: {estado}\n" +
                     $"📍 Coordenadas: {lat:F4}, {lon:F4}";

        if (!string.IsNullOrEmpty(iconoUrl))
        {
            // Si tiene foto, mostrar opciones
            var accion = await DisplayActionSheet(
                "📋 Detalles de Incidencia",
                "Cancelar",
                null,
                "Ver detalles",
                "📸 Ver Foto");

            if (accion == "Ver detalles")
            {
                await DisplayAlert("📋 Detalles de Incidencia", mensaje, "Cerrar");
            }
            else if (accion == "📸 Ver Foto")
            {
                await MostrarFotoCompletaAsync(iconoUrl);
            }
        }
        else
        {
            // Si no tiene foto, solo mostrar detalles
            await DisplayAlert("📋 Detalles de Incidencia", mensaje, "Cerrar");
        }
    }

    // ✅ NUEVO MÉTODO: Mostrar foto completa
    private async Task MostrarFotoCompletaAsync(string? fotoUrl)
    {
        if (string.IsNullOrEmpty(fotoUrl))
        {
            await DisplayAlert("Info", "Esta incidencia no tiene foto", "OK");
            return;
        }

        // Crear una página temporal para mostrar la foto
        var fotoPage = new ContentPage
        {
            Title = "Foto de la Incidencia",
            BackgroundColor = Colors.Black,
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Image
                    {
                        Source = ImageSource.FromUri(new Uri(fotoUrl)),
                        Aspect = Aspect.AspectFit,
                        MaximumHeightRequest = 500,
                        MaximumWidthRequest = 500,
                        BackgroundColor = Colors.Black
                    },
                    new Button
                    {
                        Text = "Cerrar",
                        BackgroundColor = Colors.Red,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        CornerRadius = 8,
                        Margin = new Thickness(0, 20, 0, 0)
                    }
                    .Invoke(btn => btn.Clicked += async (s, e) => await Navigation.PopModalAsync())
                }
            }
        };

        await Navigation.PushModalAsync(fotoPage);
    }

    // ✅ Método para cargar incidencias desde la API
    private async Task CargarIncidenciasAsync()
    {
        try
        {
            // ✅ SOLUCIÓN: Recrear la capa en lugar de limpiar cache
            var nuevaCapa = new MemoryLayer
            {
                Name = "Incidencias",
                Features = new List<IFeature>(),
                IsMapInfoLayer = true
            };
            var lista = await _apiService.GetIncidenciasAsync();
            var features = new List<IFeature>();

            foreach (var item in lista)
            {
                var (x, y) = SphericalMercator.FromLonLat(item.Lon, item.Lat);

                IStyle estilo;

                if (!string.IsNullOrEmpty(item.IconoUrl))
                {
                    try
                    {
                        var http = new HttpClient();
                        var bytes = await http.GetByteArrayAsync(item.IconoUrl);
                        var bitmapId = BitmapRegistry.Instance.Register(bytes);

                        // ✅ FORMA CORRECTA - Tamaño fijo en Mapsui
                        estilo = new SymbolStyle
                        {
                            BitmapId = bitmapId,
                            SymbolScale = 0.08, // ← Este es el control principal del tamaño
                            UnitType = UnitType.Pixel, // ← Mantener en píxeles
                        };

                        Console.WriteLine($"✅ Icono cargado - Tamaño fijo para incidencia {item.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error cargando icono: {ex.Message}");
                        estilo = CrearEstiloPorDefecto();
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ No hay icono para incidencia {item.Id}");
                    estilo = CrearEstiloPorDefecto();
                }

                var feature = new PointFeature(new MPoint(x, y))
                {
                    ["Descripcion"] = item.Descripcion,
                    ["CategoriaId"] = item.CategoriaId,
                    ["Estado"] = item.Estado,
                    ["IconoUrl"] = item.IconoUrl,
                    ["Id"] = item.Id // ← Agregar ID también
                };

                feature.Styles.Add(estilo);
                features.Add(feature);
            }

            pinLayer.Features = features;
            mapControl.Refresh();

            Console.WriteLine($"🗺️ Mapa actualizado con {features.Count} incidencias (tamaño fijo)");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar las incidencias: {ex.Message}", "OK");
        }
    }

    private IStyle CrearEstiloPorDefecto()
    {
        // ✅ Tamaño fijo también para íconos por defecto
        return new SymbolStyle
        {
            Fill = new Brush(Color.Red),
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 0.15, // ← Controla el tamaño del círculo
            UnitType = UnitType.Pixel,
            Line = new Pen(Color.DarkRed, 2)
        };
    }

    // =========================
    // 🚩 EVENTOS DE MENÚ
    // =========================
    private void OnAgregarIncidenciaClicked(object? sender, EventArgs e)
    {
        modoAgregar = true;
        MenuContainer.Content = CrearVistaIncidencias(); // Cambiar a listado de incidencias
    }

    private async void OnBuscarIncidenciaClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new BuscarPage());
    }

    private async void OnIrPerfilClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new PerfilPage());
    }

    private async void OnIncidenciaMenuClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Incidencia incidencia)
        {
            string nuevoNombre = await DisplayPromptAsync(
                "Editar incidencia",
                "Nuevo nombre:",
                initialValue: incidencia.Titulo);

            if (!string.IsNullOrWhiteSpace(nuevoNombre))
            {
                incidencia.Titulo = nuevoNombre;

                // 🔄 Forzar refresco en la lista
                var index = incidencias.IndexOf(incidencia);
                incidencias.RemoveAt(index);
                incidencias.Insert(index, incidencia);
            }
        }
    }

    // 🚪 Salir - Redirigir al Login
    private async void OnSalirClicked(object? sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Cerrar sesión",
            "¿Estás seguro de que querés salir?",
            "Sí, salir",
            "Cancelar"
        );

        if (confirmar)
        {
            // Cerrar sesión
            SesionUsuario.CerrarSesion();

            // Redirigir al Login
            if (Application.Current is App app)
            {
                app.SetMainPage(new LoginPage());
            }
        }
    }

    private View CrearVistaMenu()
    {
        var lblTitulo = new Label
        {
            Text = "📍 Menú",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Black
        };

        var btnAgregar = new Button
        {
            Text = "➕ Agregar incidencia",
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"),
            TextColor = Colors.White,
            CornerRadius = 12,
            FontAttributes = FontAttributes.Bold
        };
        btnAgregar.Clicked += OnAgregarIncidenciaClicked;

        var btnBuscar = new Button
        {
            Text = "🔎 Buscar incidencia",
            Margin = new Thickness(0, 10, 0, 0),
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
            CornerRadius = 12
        };
        btnBuscar.Clicked += OnBuscarIncidenciaClicked;

        var btnPerfil = new Button
        {
            Text = "👤 Ir a mi perfil",
            Margin = new Thickness(0, 10, 0, 0),
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
            CornerRadius = 12
        };
        btnPerfil.Clicked += OnIrPerfilClicked;

        var btnSalir = new Button
        {
            Text = "🚪 Salir",
            Margin = new Thickness(0, 10, 0, 0),
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
            CornerRadius = 12
        };
        btnSalir.Clicked += OnSalirClicked; // ← CAMBIADO

        var separador = new BoxView
        {
            HeightRequest = 1,
            Color = Colors.LightGray,
            Margin = new Thickness(0, 10)
        };

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
    {
        lblTitulo,
        btnAgregar,
        btnBuscar,
        btnPerfil,
        separador,
        btnSalir
    }
        };
    }

    private View CrearVistaIncidencias()
    {
        var lblTitulo = new Label
        {
            Text = "📋 Incidencias",
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Black
        };

        var lista = new CollectionView
        {
            ItemsSource = incidencias,
            ItemTemplate = new DataTemplate(() =>
            {
                var frame = new Frame
                {
                    BorderColor = Colors.LightGray,
                    CornerRadius = 8,
                    Padding = 5,
                    Margin = 5,
                    BackgroundColor = Colors.White
                };

                var title = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Black
                };
                title.SetBinding(Label.TextProperty, "Titulo");

                var coords = new Label
                {
                    FontSize = 12,
                    TextColor = Colors.Gray
                };
                coords.SetBinding(Label.TextProperty, "Coordenadas");

                var button = new Button
                {
                    Text = "⋮",
                    FontSize = 18,
                    BackgroundColor = Colors.Transparent,
                    TextColor = Colors.Black
                };
                button.SetBinding(Button.CommandParameterProperty, ".");
                button.Clicked += OnIncidenciaMenuClicked;

                var grid = new Grid
                {
                    ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
                };

                grid.Add(title, 0, 0);
                grid.Add(button, 1, 0);

                frame.Content = new VerticalStackLayout
                {
                    Children = { grid, coords }
                };

                return frame;
            })
        };

        // 🔴 Botón visible y contrastado
        var btnVolver = new Button
        {
            Text = "⬅ Volver al menú",
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"),
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        btnVolver.Clicked += (s, e) => MenuContainer.Content = CrearVistaMenu();

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children = { lblTitulo, lista, btnVolver }
        };
    }

    private View CrearVistaBusqueda()
    {
        var entry = new Entry { Placeholder = "Buscar por nombre..." };
        var date = new DatePicker();

        var btnBuscar = new Button { Text = "Buscar" };
        btnBuscar.Clicked += (s, e) =>
        {
            var query = entry.Text;
            var fecha = date.Date;
            DisplayAlert("Buscar", $"Nombre: {query}, Fecha: {fecha:dd/MM/yyyy}", "OK");
        };

        var btnVolver = new Button { Text = "⬅ Volver al menú" };
        btnVolver.Clicked += (s, e) => MenuContainer.Content = CrearVistaMenu();

        return new VerticalStackLayout
        {
            Children =
        {
            new Label { Text = "🔎 Buscar Incidencia", FontAttributes = FontAttributes.Bold, FontSize = 18, HorizontalOptions = LayoutOptions.Center },
            entry,
            date,
            btnBuscar,
            btnVolver
        }
        };
    }

    private async Task MostrarFormularioIncidenciaAsync(MPoint posicion)
    {
        var (lon, lat) = SphericalMercator.ToLonLat(posicion.X, posicion.Y);
        await Navigation.PushModalAsync(new NuevaIncidenciaPage(lat, lon));
    }
}

// Clase de extensión para el método Invoke (para hacer fluent API)
public static class ControlExtensions
{
    public static T Invoke<T>(this T control, Action<T> action) where T : VisualElement
    {
        action(control);
        return control;
    }
}

public class Incidencia
{
    public string Titulo { get; set; } = string.Empty;
    public string Coordenadas { get; set; } = string.Empty;
}