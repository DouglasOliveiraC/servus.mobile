namespace Servus.Mobile.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens the session saved by a previous login (refreshing the access token if needed)
    /// so the app does not ask for the password on every launch.
    /// </summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);

    Task<CurrentUser?> GetCurrentUserAsync();

    Task LogoutAsync();
}
