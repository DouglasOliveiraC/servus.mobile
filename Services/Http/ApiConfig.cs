namespace Servus.Mobile.Services.Http;

public static class ApiConfig
{
    /// <summary>
    /// Emulador Android não enxerga "localhost" da máquina host; 10.0.2.2 é o
    /// alias padrão do emulador para o loopback do host. Em dispositivo físico
    /// ou outras plataformas isso precisará ser trocado por um IP de rede real.
    /// </summary>
    public static string BaseUrl =>
#if ANDROID
        "http://10.0.2.2:5080/";
#else
        "http://localhost:5080/";
#endif
}
