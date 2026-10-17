namespace Servus.Mobile.Services.Auth;

public interface ITokenStore
{
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task<string?> GetTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task ClearAsync();
}
