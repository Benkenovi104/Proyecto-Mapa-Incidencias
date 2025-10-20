namespace IntegrarMapa.Helpers;

public static class SesionUsuario
{
    private const string UserIdKey = "UserId";

    public static int UserId => Preferences.Get(UserIdKey, 0);
    public static bool EstaLogueado => UserId > 0;

    public static void IniciarSesion(int userId)
    {
        Preferences.Set(UserIdKey, userId);
    }

    public static void CerrarSesion()
    {
        Preferences.Remove(UserIdKey);
    }
}