using Mapsui;
using Mapsui.UI.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

namespace IntegrarMapa
{
    public class MainPageOperador : MainPage
    {
        public MainPageOperador() : base(2) // 👈 tipoUsuario = 1 (operador)
        {
            AgregarPanelInferior();
        }

        private void AgregarPanelInferior()
        {
            var panelInferior = new Grid
            {
                BackgroundColor = Colors.White,
                Padding = new Thickness(10),
                HeightRequest = 70,
                VerticalOptions = LayoutOptions.End
            };

            // Definir 3 columnas iguales
            panelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            panelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            panelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            // Botón 1
            var btnIncidencias = new Button
            {
                Text = "📋 Panel de Incidencias",
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            btnIncidencias.Clicked += (s, e) => MostrarPanelIncidencias();

            // Botón 2
            var btnUsuarios = new Button
            {
                Text = "👥 Gestión de Usuarios",
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                TextColor = Colors.Black,
                CornerRadius = 10
            };
            btnUsuarios.Clicked += (s, e) => MostrarGestionUsuarios();

            // Botón 3
            var btnOperador = new Button
            {
                Text = "➕ Agregar Operador",
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                TextColor = Colors.Black,
                CornerRadius = 10
            };
            btnOperador.Clicked += (s, e) => MostrarFormularioOperador();

            // Agregar los botones al grid
            panelInferior.Add(btnIncidencias, 0, 0);
            panelInferior.Add(btnUsuarios, 1, 0);
            panelInferior.Add(btnOperador, 2, 0);

            // Posicionar el panel en la parte inferior
            AbsoluteLayout.SetLayoutFlags(panelInferior,
                AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.WidthProportional);
            AbsoluteLayout.SetLayoutBounds(panelInferior, new Rect(0, 1, 1, 70));

            // Insertar dentro del AbsoluteLayout principal del MainPage
            (Content as AbsoluteLayout)?.Children.Add(panelInferior);
        }

        // 🚧 Métodos vacíos por ahora, los implementaremos después
        private async void MostrarPanelIncidencias()
        {
            await Navigation.PushModalAsync(new PanelIncidenciasPage());
        }

        //private void MostrarGestionUsuarios()
        //{
        //    DisplayAlert("Gestión de usuarios", "Aquí podrás administrar los usuarios.", "OK");
        //}
        private async void MostrarGestionUsuarios()
        {
            await Navigation.PushModalAsync(new GestionUsuariosPage());
        }

        private void MostrarFormularioOperador()
        {
            DisplayAlert("Agregar operador", "Aquí se abrirá el formulario para crear un nuevo operador.", "OK");
        }
    }
}
