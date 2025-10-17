namespace IntegrarMapa.Helpers
{
    public static class SesionUsuario
    {
        public static int UserId { get; private set; }
        public static string? Token { get; private set; }

        public static void IniciarSesion(int userId, string? token = null)
        {
            UserId = userId;
            Token = token;
        }

        public static void CerrarSesion()
        {
            UserId = 0;
            Token = null;
        }

        public static bool EstaLogueado => UserId > 0;
    }
}
