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

        map.Navigator.ZoomTo(5000); // 🔥 ZOOM MUCHO MÁS ALEJADO para ver el mundo

        mapControl.Map = map;
        mapControl.Info += OnMapTapped;

        // Cargar el menú inicial
        MenuContainer.Content = CrearVistaMenu();

        Console.WriteLine("🗺️ Mapa inicializado - Vista Mundial");
    }

    // 🎯 MÉTODOS PARA ZOOM MANUAL
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarIncidenciasAsync();
    }

    // 🔥 CORREGIDO: Remover nullable del sender
    private async void OnMapTapped(object sender, MapInfoEventArgs e)
    {
        if (e.MapInfo?.Feature is PointFeature pointFeature)
        {
            var descripcion = pointFeature["Descripcion"]?.ToString() ?? "Sin descripción";
            var estado = pointFeature["Estado"]?.ToString() ?? "Desconocido";
            var iconoUrl = pointFeature["IconoUrl"]?.ToString() ?? "";
            var fotoUrl = pointFeature["FotoUrl"]?.ToString() ?? ""; // ✅ Obtener la foto real

            var point = pointFeature.Point;
            var (lon, lat) = SphericalMercator.ToLonLat(point.X, point.Y);

            Console.WriteLine($"📍 Ícono tocado: {descripcion} en {lat}, {lon}");

            // ✅ CORREGIDO: Pasar fotoUrl al método
            await MostrarDetalleIncidenciaAsync(descripcion, estado, lat, lon, iconoUrl, fotoUrl);
        }
        else if (modoAgregar && e.MapInfo?.WorldPosition is MPoint pos)
        {
            modoAgregar = false;
            await MostrarFormularioIncidenciaAsync(pos);
        }
    }

    // 🔥 CORREGIDO: Remover nullable del parámetro
    private async Task MostrarDetalleIncidenciaAsync(string descripcion, string estado, double lat, double lon, string iconoUrl, string fotoUrl)
    {
        var mensaje = $"📝 {descripcion}\n\n🔄 Estado: {estado}\n📍 Coordenadas: {lat:F4}, {lon:F4}";

        // ✅ CORREGIDO: Verificar si tiene foto real (FotoUrl) en lugar del icono
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
            // Si no tiene foto, solo mostrar detalles
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
            // ✅ CORREGIDO: Usar StackLayout en lugar de Grid para simplificar
            var fotoPage = new ContentPage
            {
                Title = "Foto de la Incidencia",
                BackgroundColor = Colors.Black,
                Content = new StackLayout
                {
                    Spacing = 0,
                    Children =
                {
                    // Imagen que ocupa la mayor parte de la pantalla
                    new Image
                    {
                        Source = ImageSource.FromUri(new Uri(fotoUrl)),
                        Aspect = Aspect.AspectFit,
                        BackgroundColor = Colors.Black,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill
                    },
                    // Botón cerrar en la parte inferior
                    new Button
                    {
                        Text = "✕ Cerrar",
                        BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#CC000000"),
                        TextColor = Colors.White,
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        CornerRadius = 20,
                        Margin = new Thickness(20),
                        Padding = new Thickness(20, 10),
                        HorizontalOptions = LayoutOptions.Center
                    }.Invoke(btn => btn.Clicked += async (s, e) => await Navigation.PopModalAsync())
                }
                }
            };

            await Navigation.PushModalAsync(fotoPage);
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
                    ["FotoUrl"] = item.FotoUrl, // ✅ NUEVO: Agregar la URL de la foto real
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
                    // 🔥 CORREGIDO: Usar Microsoft.Maui.Graphics.Color.FromArgb con formato hexadecimal
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"),
                    TextColor = Colors.White,
                    CornerRadius = 12,
                    FontAttributes = FontAttributes.Bold
                }.Invoke(btn => btn.Clicked += OnAgregarIncidenciaClicked),
                new Button
                {
                    Text = "🔎 Buscar incidencia",
                    Margin = new Thickness(0, 10, 0, 0),
                    // 🔥 CORREGIDO: Usar Microsoft.Maui.Graphics.Color.FromArgb con formato hexadecimal
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#333333"),
                    CornerRadius = 12
                }.Invoke(btn => btn.Clicked += OnBuscarIncidenciaClicked),
                new Button
                {
                    Text = "👤 Ir a mi perfil",
                    Margin = new Thickness(0, 10, 0, 0),
                    // 🔥 CORREGIDO: Usar Microsoft.Maui.Graphics.Color.FromArgb con formato hexadecimal
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
                    // 🔥 CORREGIDO: Usar Microsoft.Maui.Graphics.Color.FromArgb con formato hexadecimal
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
                // 🔥 CORREGIDO: Usar Microsoft.Maui.Graphics.Color.FromArgb con formato hexadecimal
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