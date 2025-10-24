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
using Microsoft.Maui.Graphics;
#nullable disable
namespace IntegrarMapa;

public partial class MainPage : ContentPage
{
    private bool modoAgregar = false;
    private readonly MemoryLayer pinLayer;
    private readonly ObservableCollection<Incidencia> incidencias = [];
    private readonly int tipoUsuario;
    private readonly ApiService _apiService = new();

    // 🔍 NUEVAS VARIABLES PARA LUPA VISUAL
    private bool lupaActiva = false;
    private Frame lupaVisual;
    private Image imagenLupa;
    private double nivelZoomLupa = 2.0;

    public MainPage(int tipoUsuario = 2)
    {
        InitializeComponent();
        this.tipoUsuario = tipoUsuario;

        // 🔥 CORREGIDO: Usar Mapsui.Map explícitamente
        var map = new Mapsui.Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Capa de pines
        pinLayer = new MemoryLayer
        {
            Name = "Incidencias",
            Features = [],
            IsMapInfoLayer = true
        };
        map.Layers.Add(pinLayer);

        // Configuración de controles táctiles
        mapControl.UseDoubleTap = true;
        mapControl.UseFling = true;

        // Alternativa: usar ZoomLevel en lugar de ZoomTo
        var (x, y) = SphericalMercator.FromLonLat(-64.1888, -31.4201);
        map.Home = n => n.CenterOnAndZoomTo(new MPoint(x, y), n.Resolutions[10]); // Nivel 10 para vista de ciudad

        map.Navigator.ZoomTo(5000);

        mapControl.Map = map;
        mapControl.Info += OnMapTapped;

        // 🔍 AGREGAR ESTA LÍNEA PARA CONECTAR EL TAP AL ICONO DE LUPA
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnLupaIconoTapped;
        LupaContainer.GestureRecognizers.Add(tapGesture);

        // Cargar el menú inicial
        MenuContainer.Content = CrearVistaMenu();

        Console.WriteLine("🗺️ Mapa inicializado - Vista Mundial");
    }

    // 🎯 MÉTODOS PARA ZOOM MANUAL DEL MAPA
    private void OnZoomInClicked(object sender, EventArgs e)
    {
        try
        {
            mapControl.Map.Navigator.ZoomIn(300);
            mapControl.Refresh();
            Console.WriteLine("🔍 Zoom In (acercar)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en Zoom In: {ex.Message}");
        }
    }

    private void OnZoomOutClicked(object sender, EventArgs e)
    {
        try
        {
            mapControl.Map.Navigator.ZoomOut(300);
            mapControl.Refresh();
            Console.WriteLine("🔍 Zoom Out (alejar)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en Zoom Out: {ex.Message}");
        }
    }

    // 🔍 MÉTODOS PARA LUPA DE ACCESIBILIDAD VISUAL
    private async void OnActivarLupaClicked(object sender, EventArgs e)
    {
        lupaActiva = !lupaActiva;

        if (lupaActiva)
        {
            await ActivarLupaVisual();
            await DisplayAlert("🔍 Lupa Activada",
                "La lupa de accesibilidad está activa. Mové el dedo por la pantalla para ver el área ampliada.",
                "Entendido");
        }
        else
        {
            DesactivarLupaVisual();
        }
    }

    // 🔍 AGREGAR ESTE MÉTODO PARA QUE EL ICONO DE LUPA FUNCIONE
    private async void OnLupaIconoTapped(object sender, EventArgs e)
    {
        if (lupaActiva)
        {
            // Si la lupa ya está activa, mostrar opciones
            await MostrarOpcionesZoomLupa();
        }
        else
        {
            // Si no está activa, activarla
            lupaActiva = true;
            await ActivarLupaVisual();
        }
    }
    private async Task ActivarLupaVisual()
    {
        try
        {
            // Crear la lupa visual
            CrearLupaVisual();

            // Mostrar la lupa
            lupaVisual.IsVisible = true;
            LupaContainer.IsVisible = true;

            // Configurar gestos
            ConfigurarGestosLupa();

            Console.WriteLine("🔍 Lupa visual activada");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo activar la lupa: {ex.Message}", "OK");
            lupaActiva = false;
        }
    }

    private void CrearLupaVisual()
    {
        // Crear el frame de la lupa
        lupaVisual = new Frame
        {
            WidthRequest = 200,
            HeightRequest = 200,
            BackgroundColor = Colors.White,
            BorderColor = Colors.Black,
            CornerRadius = 100,
            HasShadow = true,
            IsVisible = false,
            Padding = 5,
            ZIndex = 1002
        };

        // Crear la imagen para la lupa (sin CornerRadius que no existe en Image)
        imagenLupa = new Image
        {
            Aspect = Aspect.AspectFill,
            BackgroundColor = Colors.White,
            WidthRequest = 190,
            HeightRequest = 190
        };

        lupaVisual.Content = imagenLupa;

        // Agregar la lupa al layout principal
        if (this.Content is AbsoluteLayout absoluteLayout)
        {
            absoluteLayout.Children.Add(lupaVisual);
        }
    }

    private void ConfigurarGestosLupa()
    {
        // Limpiar gestos anteriores
        this.Content.GestureRecognizers.Clear();

        // Gesto de pan para mover la lupa
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanLupaUpdated;
        this.Content.GestureRecognizers.Add(panGesture);

        // Gesto de tap en el icono de lupa para opciones
        LupaContainer.GestureRecognizers.Clear();
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) => {
            await MostrarOpcionesZoomLupa();
        };
        LupaContainer.GestureRecognizers.Add(tapGesture);
    }

    private void OnPanLupaUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (!lupaActiva) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // Mostrar la lupa en la posición inicial
                if (lupaVisual != null)
                {
                    lupaVisual.TranslationX = e.TotalX - 100; // Centrar la lupa
                    lupaVisual.TranslationY = e.TotalY - 100;
                    lupaVisual.IsVisible = true;

                    // Actualizar la imagen de la lupa (simulación)
                    ActualizarImagenLupa();
                }
                break;

            case GestureStatus.Running:
                // Mover la lupa siguiendo el dedo
                if (lupaVisual != null)
                {
                    lupaVisual.TranslationX = e.TotalX - 100;
                    lupaVisual.TranslationY = e.TotalY - 100;

                    // Actualizar la imagen mientras se mueve
                    ActualizarImagenLupa();
                }
                break;

            case GestureStatus.Completed:
                // Ocultar la lupa al terminar
                if (lupaVisual != null)
                {
                    lupaVisual.IsVisible = false;
                }
                break;
        }
    }

    private void ActualizarImagenLupa()
    {
        // En una implementación real, aquí capturarías una imagen de la pantalla
        // en la posición actual y la mostrarías ampliada en la lupa
        // Por ahora, usamos un placeholder visual

        // Simular efecto de zoom cambiando el color de fondo
        var colors = new[] { Colors.LightBlue, Colors.LightGreen, Colors.LightYellow, Colors.LightPink };
        var randomColor = colors[new Random().Next(colors.Length)];

        imagenLupa.BackgroundColor = randomColor;

        // Mostrar información de posición (para debugging)
        Console.WriteLine($"🔍 Lupa en posición: X={lupaVisual.TranslationX + 100}, Y={lupaVisual.TranslationY + 100}");
    }

    private async Task MostrarOpcionesZoomLupa()
    {
        var zoomLevel = await DisplayActionSheet("🔍 Nivel de Zoom Lupa", "Cancelar", null,
            "1.5x", "2x", "2.5x", "3x", "Apagar Lupa");

        if (zoomLevel != "Cancelar")
        {
            switch (zoomLevel)
            {
                case "1.5x": nivelZoomLupa = 1.5; break;
                case "2x": nivelZoomLupa = 2.0; break;
                case "2.5x": nivelZoomLupa = 2.5; break;
                case "3x": nivelZoomLupa = 3.0; break;
                case "Apagar Lupa":
                    lupaActiva = false;
                    DesactivarLupaVisual();
                    return;
            }

            await DisplayAlert("🔍 Lupa", $"Zoom ajustado a {nivelZoomLupa}x", "OK");
        }
    }

    private void DesactivarLupaVisual()
    {
        // Ocultar y limpiar la lupa visual
        if (lupaVisual != null)
        {
            lupaVisual.IsVisible = false;
        }

        LupaContainer.IsVisible = false;

        // Limpiar gestos
        this.Content.GestureRecognizers.Clear();
        LupaContainer.GestureRecognizers.Clear();

        Console.WriteLine("🔍 Lupa visual desactivada");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarIncidenciasAsync();
    }

    private async void OnMapTapped(object sender, MapInfoEventArgs e)
    {
        if (e.MapInfo?.Feature is PointFeature pointFeature)
        {
            var descripcion = pointFeature["Descripcion"]?.ToString() ?? "Sin descripción";
            var estado = pointFeature["Estado"]?.ToString() ?? "Desconocido";
            var iconoUrl = pointFeature["IconoUrl"]?.ToString() ?? "";
            var fotoUrl = pointFeature["FotoUrl"]?.ToString() ?? "";

            var point = pointFeature.Point;
            var (lon, lat) = SphericalMercator.ToLonLat(point.X, point.Y);

            Console.WriteLine($"📍 Ícono tocado: {descripcion} en {lat}, {lon}");

            await MostrarDetalleIncidenciaAsync(descripcion, estado, lat, lon, iconoUrl, fotoUrl);
        }
        else if (modoAgregar && e.MapInfo?.WorldPosition is MPoint pos)
        {
            modoAgregar = false;
            await MostrarFormularioIncidenciaAsync(pos);
        }
    }

    private async Task MostrarDetalleIncidenciaAsync(string descripcion, string estado, double lat, double lon, string iconoUrl, string fotoUrl)
    {
        var mensaje = $"📝 {descripcion}\n\n🔄 Estado: {estado}\n📍 Coordenadas: {lat:F4}, {lon:F4}";

        if (!string.IsNullOrEmpty(fotoUrl))
        {
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
                await MostrarFotoCompletaAsync(fotoUrl);
            }
        }
        else
        {
            await DisplayAlert("📋 Detalles de Incidencia", mensaje, "Cerrar");
        }
    }

    private async Task MostrarFotoCompletaAsync(string fotoUrl)
    {
        if (string.IsNullOrEmpty(fotoUrl))
        {
            await DisplayAlert("Info", "Esta incidencia no tiene foto", "OK");
            return;
        }

        try
        {
            // Crear la imagen
            var image = new Image
            {
                Source = ImageSource.FromUri(new Uri(fotoUrl)),
                Aspect = Aspect.AspectFit,
                BackgroundColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            // Crear el botón cerrar
            var closeButton = new Button
            {
                Text = "✕ Cerrar",
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#CC000000"),
                TextColor = Colors.White,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 10,
                Margin = new Thickness(20),
                Padding = new Thickness(20, 10),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            closeButton.Clicked += async (s, e) =>
            {
                Console.WriteLine("🔙 Cerrando vista de foto...");
                await Navigation.PopModalAsync();
            };

            // Crear el Grid y asignar las filas
            var grid = new Grid
            {
                RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(70, GridUnitType.Absolute) }
            }
            };

            // Agregar elementos al grid y asignar las filas
            grid.Add(image);
            grid.Add(closeButton);

            Grid.SetRow(image, 0);
            Grid.SetRow(closeButton, 1);

            var fotoPage = new ContentPage
            {
                Title = "Foto de la Incidencia",
                BackgroundColor = Colors.Black,
                Content = grid
            };

            // Usar PushModalAsync con NavigationPage para mejor manejo
            await Navigation.PushModalAsync(new NavigationPage(fotoPage)
            {
                BarBackgroundColor = Colors.Black,
                BarTextColor = Colors.White
            });

            Console.WriteLine($"📸 Mostrando foto: {fotoUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al mostrar foto: {ex.Message}");
            await DisplayAlert("Error", "No se pudo cargar la foto", "OK");
        }
    }

    private async Task CargarIncidenciasAsync()
    {
        try
        {
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

                        estilo = new SymbolStyle
                        {
                            BitmapId = bitmapId,
                            SymbolScale = 0.08,
                            UnitType = UnitType.Pixel,
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error cargando icono: {ex.Message}");
                        estilo = CrearEstiloPorDefecto();
                    }
                }
                else
                {
                    estilo = CrearEstiloPorDefecto();
                }

                var feature = new PointFeature(new MPoint(x, y))
                {
                    ["Descripcion"] = item.Descripcion,
                    ["CategoriaId"] = item.CategoriaId,
                    ["Estado"] = item.Estado,
                    ["IconoUrl"] = item.IconoUrl,
                    ["FotoUrl"] = item.FotoUrl,
                    ["Id"] = item.Id
                };

                feature.Styles.Add(estilo);
                features.Add(feature);
            }

            pinLayer.Features = features;
            mapControl.Refresh();

            Console.WriteLine($"🗺️ Mapa actualizado con {features.Count} incidencias");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar las incidencias: {ex.Message}", "OK");
        }
    }

    private IStyle CrearEstiloPorDefecto()
    {
        return new SymbolStyle
        {
            Fill = new Brush(Color.Red),
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 0.15,
            UnitType = UnitType.Pixel,
            Line = new Pen(Color.DarkRed, 2)
        };
    }

    // =========================
    // 🚩 EVENTOS DE MENÚ
    // =========================
    private void OnAgregarIncidenciaClicked(object sender, EventArgs e)
    {
        modoAgregar = true;
        MenuContainer.Content = CrearVistaIncidencias;
    }

    private async void OnBuscarIncidenciaClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new BuscarPage());
    }

    private async void OnIrPerfilClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new PerfilPage());
    }

    private async void OnSalirClicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cerrar sesión", "¿Estás seguro de que querés salir?", "Sí, salir", "Cancelar");

        if (confirmar)
        {
            SesionUsuario.CerrarSesion();
            if (Application.Current is App app)
            {
                app.SetMainPage(new LoginPage());
            }
        }
    }

    private View CrearVistaMenu()
    {
        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "📍 Menú",
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black
                },
                new Button
                {
                    Text = "➕ Agregar incidencia",
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"),
                    TextColor = Colors.White,
                    CornerRadius = 12,
                    FontAttributes = FontAttributes.Bold
                }.Invoke(btn => btn.Clicked += OnAgregarIncidenciaClicked),
                new Button
                {
                    Text = "🔎 Buscar incidencia",
                    Margin = new Thickness(0, 10, 0, 0),
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
                    CornerRadius = 12
                }.Invoke(btn => btn.Clicked += OnBuscarIncidenciaClicked),
                new Button
                {
                    Text = "👤 Ir a mi perfil",
                    Margin = new Thickness(0, 10, 0, 0),
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
                    CornerRadius = 12
                }.Invoke(btn => btn.Clicked += OnIrPerfilClicked),
                new BoxView
                {
                    HeightRequest = 1,
                    Color = Colors.LightGray,
                    Margin = new Thickness(0, 10)
                },
                new Button
                {
                    Text = "🚪 Salir",
                    Margin = new Thickness(0, 10, 0, 0),
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
                    CornerRadius = 12
                }.Invoke(btn => btn.Clicked += OnSalirClicked)
            }
        };
    }

    private View CrearVistaIncidencias
    {
        get
        {
            var btnVolver = new Button
            {
                Text = "⬅ Volver al menú",
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"),
                TextColor = Colors.White,
                CornerRadius = 10,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            }.Invoke(btn => btn.Clicked += (s, e) => MenuContainer.Content = CrearVistaMenu());

            return new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "📋 Incidencias",
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 18,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = Colors.Black
                    },
                    new CollectionView
                    {
                        ItemsSource = incidencias,
                        ItemTemplate = new DataTemplate(() =>
                        {
                            var title = new Label { FontAttributes = FontAttributes.Bold, TextColor = Colors.Black };
                            title.SetBinding(Label.TextProperty, "Titulo");

                            var coords = new Label { FontSize = 12, TextColor = Colors.Gray };
                            coords.SetBinding(Label.TextProperty, "Coordenadas");

                            var button = new Button { Text = "⋮", FontSize = 18, BackgroundColor = Colors.Transparent, TextColor = Colors.Black };
                            button.SetBinding(Button.CommandParameterProperty, ".");

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

                            return new Frame
                            {
                                BorderColor = Colors.LightGray,
                                CornerRadius = 8,
                                Padding = 5,
                                Margin = 5,
                                BackgroundColor = Colors.White,
                                Content = new VerticalStackLayout { Children = { grid, coords } }
                            };
                        })
                    },
                    btnVolver
                }
            };
        }
    }

    private async Task MostrarFormularioIncidenciaAsync(MPoint posicion)
    {
        var (lon, lat) = SphericalMercator.ToLonLat(posicion.X, posicion.Y);
        await Navigation.PushModalAsync(new NuevaIncidenciaPage(lat, lon));
    }
}

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