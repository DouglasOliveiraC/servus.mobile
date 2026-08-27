namespace Servus.Mobile.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default);

    Task<CurrentUser?> GetCurrentUserAsync();

    Task LogoutAsync();
}
