namespace Servus.Mobile.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a throwaway sandbox pre-filled with sample data, without an account. The API deletes
    /// it when it expires, after which the session simply stops refreshing.
    /// </summary>
    Task<AuthResult> LoginAsDemoAsync(CancellationToken cancellationToken = default);

    Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reopens the session saved by a previous login (refreshing the access token if needed)
    /// so the app does not ask for the password on every launch.
    /// </summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);

    Task<CurrentUser?> GetCurrentUserAsync();

    /// <summary>
    /// Best effort, never throws: wakes the API and its database while the person is still on the
    /// login screen, so signing in does not pay for both cold starts.
    /// </summary>
    Task WarmUpAsync();

    Task LogoutAsync();
}
