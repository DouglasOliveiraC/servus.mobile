using System.Net.Http.Json;
using Servus.Mobile.Services.Telemetry;
#if ANDROID
using Android.Gms.Extensions;
using Firebase.Messaging;
#endif

namespace Servus.Mobile.Services.Push;

public class PushRegistrationService : IPushRegistrationService
{
    private readonly HttpClient _httpClient;
    private readonly ITelemetryService _telemetry;

    public PushRegistrationService(HttpClient httpClient, ITelemetryService telemetry)
    {
        _httpClient = httpClient;
        _telemetry = telemetry;
    }

    // Push is a convenience on top of the app: no failure here (no Firebase config, offline,
    // demo account refused by the API) may surface to the user or break the flow that called it.
    public async Task RegisterAsync(string? token = null)
    {
        try
        {
            token ??= await GetDeviceTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            using var _ = await _httpClient.PostAsJsonAsync("api/push-devices", new { Token = token, Platform = "android" });
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
        }
    }

    public async Task UnregisterAsync()
    {
        try
        {
            var token = await GetDeviceTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            using var _ = await _httpClient.PostAsJsonAsync("api/push-devices/unregister", new { Token = token });
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
        }
    }

    private static async Task<string?> GetDeviceTokenAsync()
    {
#if ANDROID
        // Throws IllegalStateException when the build had no google-services.json (FirebaseApp
        // never initialized); the callers' catch turns that into "no push".
        var token = await FirebaseMessaging.Instance.GetToken().AsAsync<Java.Lang.String>();
        return token?.ToString();
#else
        return await Task.FromResult<string?>(null);
#endif
    }
}
