using IntegrarMapa.Helpers;
using IntegrarMapa.Models;
using IntegrarMapa.Services;
using System.Net.Http.Json;

namespace IntegrarMapa;

public partial class NuevaIncidenciaPage : ContentPage
{
    private readonly double _lat;
    private readonly double _lon;
    private List<CategoriaDto>? _categorias;
    private readonly ApiService _apiService = new();
    private byte[]? _fotoBytes;
    private string? _fotoUrl;

    public NuevaIncidenciaPage(double lat, double lon)
    {
        InitializeComponent();
        _lat = lat;
        _lon = lon;

        lblCoordenadas.Text = $"📍 Lat: {_lat:F5}, Lon: {_lon:F5}";

        // Ocultar preview inicialmente
        imgPreview.IsVisible = false;

        _ = CargarCategoriasAsync();
    }

    private async Task CargarCategoriasAsync()
    {
        try
        {
            _categorias = await _apiService.GetCategoriasAsync();

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

    // 📸 MÉTODO UNIFICADO PARA OBTENER FOTO
    private async void OnTomarFotoClicked(object sender, EventArgs e)
    {
        try
        {
            // Opciones para el usuario
            var action = await DisplayActionSheet(
                "Seleccionar foto",
                "Cancelar",
                null,
                "📷 Tomar foto",
                "📁 Seleccionar de galería");

            if (action == "📷 Tomar foto")
            {
                await TomarFotoConCamara();
            }
            else if (action == "📁 Seleccionar de galería")
            {
                await SeleccionarFotoDeGaleria();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo obtener la foto:\n{ex.Message}", "OK");
        }
    }

    private async Task TomarFotoConCamara()
    {
        try
        {
            // Verificar si estamos en Windows (donde no hay cámara)
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                await DisplayAlert("Info", "La cámara no está disponible en Windows. Usa 'Seleccionar de galería'.", "OK");
                return;
            }

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permiso requerido", "Se necesita permiso de cámara para tomar fotos", "OK");
                    return;
                }
            }

            var photo = await MediaPicker.CapturePhotoAsync();

            if (photo != null)
            {
                await ProcesarFoto(photo, "tomada");
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("No compatible", "La cámara no está disponible en este dispositivo", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al usar la cámara: {ex.Message}", "OK");
        }
    }

    private async Task SeleccionarFotoDeGaleria()
    {
        try
        {
            var fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccionar foto de la incidencia",
                FileTypes = FilePickerFileType.Images
            });

            if (fileResult != null)
            {
                await ProcesarFoto(fileResult, "seleccionada");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar archivo: {ex.Message}", "OK");
        }
    }

    private async Task ProcesarFoto(FileResult photo, string tipo)
    {
        using var stream = await photo.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        _fotoBytes = memoryStream.ToArray();

        imgPreview.Source = ImageSource.FromStream(() => new MemoryStream(_fotoBytes));
        imgPreview.IsVisible = true;

        lblEstadoFoto.Text = $"✅ Foto {tipo} correctamente";
        lblEstadoFoto.TextColor = Colors.Green;

        await SubirFotoAsync();
    }

    // 📤 MÉTODO PARA SUBIR FOTO A SUPABASE
    private async Task SubirFotoAsync()
    {
        if (_fotoBytes == null || _fotoBytes.Length == 0)
        {
            await DisplayAlert("Error", "No hay foto para subir", "OK");
            return;
        }

        try
        {
            // Mostrar indicador de carga
            lblEstadoFoto.Text = "⏳ Subiendo foto...";
            lblEstadoFoto.TextColor = Colors.Orange;

            // Subir foto
            var response = await _apiService.SubirFotoAsync(
                _fotoBytes,
                $"incidencia_{DateTime.Now:yyyyMMdd_HHmmss}.jpg",
                "image/jpeg"
            );

            if (response != null && response.Success)
            {
                _fotoUrl = response.PhotoUrl;
                lblEstadoFoto.Text = "✅ Foto subida exitosamente";
                lblEstadoFoto.TextColor = Colors.Green;

                Console.WriteLine($"📸 Foto subida: {_fotoUrl}");
            }
            else
            {
                lblEstadoFoto.Text = "❌ Error al subir foto";
                lblEstadoFoto.TextColor = Colors.Red;
                await DisplayAlert("Error", "No se pudo subir la foto al servidor", "OK");
            }
        }
        catch (Exception ex)
        {
            lblEstadoFoto.Text = "❌ Error al subir foto";
            lblEstadoFoto.TextColor = Colors.Red;
            await DisplayAlert("Error", $"Error al subir foto:\n{ex.Message}", "OK");
        }
    }

    // 📝 MÉTODO ENVIAR INCIDENCIA (ACTUALIZADO)
    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        // Validaciones
        if (pickerCategoria.SelectedIndex < 0)
        {
            await DisplayAlert("Error", "Debe seleccionar una categoría", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(entryDescripcion.Text))
        {
            await DisplayAlert("Error", "Debe ingresar una descripción", "OK");
            return;
        }

        // ✅ NUEVA VALIDACIÓN: Foto obligatoria
        if (string.IsNullOrEmpty(_fotoUrl))
        {
            await DisplayAlert("Error", "Debe tomar una foto de la incidencia", "OK");
            return;
        }

        try
        {
            var categoria = _categorias![pickerCategoria.SelectedIndex];

            // Crear incidencia con la foto
            var success = await _apiService.CrearIncidenciaAsync(
                categoria.Id,
                entryDescripcion.Text,
                _lat,
                _lon,
                _fotoUrl
            );

            if (success)
            {
                await DisplayAlert("Éxito", "Incidencia registrada correctamente", "OK");
                await Navigation.PopModalAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo registrar la incidencia", "OK");
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