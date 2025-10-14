namespace IntegrarMapa;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // La primera pagina al iniciar la app
        MainPage = new NavigationPage(new LoginPage());
        //MainPage = new NavigationPage(new PerfilPage());
    }

    // Este metodo nos permite cambiar de pagina despues del login
    public void SetMainPage(Page page)
    {
        MainPage = new NavigationPage(page);
    }
}
