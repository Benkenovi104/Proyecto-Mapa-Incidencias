using Mapsui;
using Mapsui.Tiling;
using Mapsui.Projections;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI.Maui;
using System.Collections.ObjectModel;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;

namespace IntegrarMapa;

public partial class MainPage : ContentPage
{
    private bool modoAgregar = false;
    private MemoryLayer pinLayer;
    private ObservableCollection<Incidencia> incidencias;
    private int tipoUsuario; // 1 = operador, 2 = cliente


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

        // Vista inicial → Buenos Aires
        var (x, y) = SphericalMercator.FromLonLat(-58.3816, -34.6037);
        var center = new MPoint(x, y);
        map.Home = n => n.CenterOn(center);

        mapControl.Map = map;

        // Capturar clicks en el mapa
        mapControl.Info += OnMapInfo;

        // Cargar el menú inicial
        MenuContainer.Content = CrearVistaMenu();
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

    // =========================
    // 🚩 EVENTOS DEL MAPA
    // =========================
    private async void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        if (!modoAgregar) return;
        if (e.MapInfo?.WorldPosition is MPoint pos)
        {
            modoAgregar = false;
            await MostrarFormularioIncidenciaAsync(pos);
        }
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

    // =========================
    // 🚩 VISTAS DEL MENÚ
    // =========================
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
        btnSalir.Clicked += (s, e) => Application.Current?.Quit();


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
        // Crear los controles del formulario
        var entryTitulo = new Entry { Placeholder = "Título (opcional)" };

        var pickerTipo = new Picker { Title = "Tipo de incidencia" };
        pickerTipo.Items.Add("Pozo en la calle");
        pickerTipo.Items.Add("Choque automovilístico");
        pickerTipo.Items.Add("Incendio");

        var imgPreview = new Image
        {
            HeightRequest = 150,
            WidthRequest = 150,
            BackgroundColor = Colors.LightGray,
            Aspect = Aspect.AspectFill
        };

        var btnImagen = new Button
        {
            Text = "📷 Cargar o tomar foto",
            BackgroundColor = Colors.LightGray,
            TextColor = Colors.Black
        };

        FileResult? fotoSeleccionada = null;

        btnImagen.Clicked += async (s, e) =>
        {
            var action = await DisplayActionSheet("Seleccionar imagen", "Cancelar", null, "Tomar foto", "Elegir de galería");
            if (action == "Tomar foto")
            {
                fotoSeleccionada = await MediaPicker.CapturePhotoAsync();
            }
            else if (action == "Elegir de galería")
            {
                fotoSeleccionada = await MediaPicker.PickPhotoAsync();
            }

            if (fotoSeleccionada != null)
            {
                imgPreview.Source = ImageSource.FromFile(fotoSeleccionada.FullPath);
            }
        };

        var btnCrear = new Button
        {
            Text = "✅ Crear incidencia",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            CornerRadius = 10
        };

        // Mostrar formulario en un modal
        var formulario = new StackLayout
        {
            Padding = 20,
            Spacing = 10,
            Children = {
            new Label { Text = "🆕 Nueva Incidencia", FontAttributes = FontAttributes.Bold, FontSize = 20, HorizontalOptions = LayoutOptions.Center },
            entryTitulo,
            pickerTipo,
            btnImagen,
            imgPreview,
            btnCrear
        }
        };

        var contentPage = new ContentPage
        {
            Content = new ScrollView { Content = formulario }
        };

        await Navigation.PushModalAsync(contentPage);

        btnCrear.Clicked += async (s, e) =>
        {
            if (pickerTipo.SelectedIndex == -1)
            {
                await DisplayAlert("Error", "Seleccioná un tipo de incidencia.", "OK");
                return;
            }

            // Crear la nueva incidencia
            var nueva = new Incidencia
            {
                Titulo = string.IsNullOrWhiteSpace(entryTitulo.Text)
                            ? pickerTipo.SelectedItem.ToString() ?? "Sin título"
                            : entryTitulo.Text ?? "Sin título",
                Coordenadas = $"Lat: {posicion.Y:0.0000}, Lon: {posicion.X:0.0000}"
            };

            incidencias.Add(nueva);

            // Crear pin en el mapa
            var pin = new PointFeature(posicion);
            pin.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(Color.Red),
                Outline = new Pen(Color.White, 2)
            });
            (pinLayer.Features as List<IFeature>)?.Add(pin);
            mapControl.Refresh();

            MenuContainer.Content = CrearVistaIncidencias();

            await Navigation.PopModalAsync();
        };
    }

}

public class Incidencia
{
    public string Titulo { get; set; } = string.Empty;
    public string Coordenadas { get; set; } = string.Empty;
}
