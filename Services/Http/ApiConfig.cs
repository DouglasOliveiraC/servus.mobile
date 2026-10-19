namespace Servus.Mobile.Services.Http;

public static class ApiConfig
{
    // Debug builds talk to the API running on the developer's machine (10.0.2.2 is the Android
    // emulator's alias for the host loopback). Release builds are the APK published on GitHub,
    // so they can only ever point at the production API over HTTPS.
    public static string BaseUrl =>
#if DEBUG
#if ANDROID
        "http://10.0.2.2:5080/";
#else
        "http://localhost:5080/";
#endif
#else
        "https://api.servus.newsight.com.br/";
#endif
}
