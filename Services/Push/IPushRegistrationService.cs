namespace Servus.Mobile.Services.Push;

public interface IPushRegistrationService
{
    /// <summary>
    /// Tells the API this phone belongs to the signed-in user. Safe to call on every dashboard
    /// load: the API upserts by token. <paramref name="token"/> is passed when Firebase just
    /// handed out a new one; otherwise it is read from Firebase.
    /// </summary>
    Task RegisterAsync(string? token = null);

    /// <summary>
    /// Must run before the session tokens are cleared: the API only removes a device for the
    /// user who owns it, so the call has to be authenticated.
    /// </summary>
    Task UnregisterAsync();
}
