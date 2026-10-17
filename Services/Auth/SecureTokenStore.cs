namespace Servus.Mobile.Services.Auth;

public class SecureTokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";
    private const string RefreshTokenKey = "auth_refresh_token";

    public async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(TokenKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
    }

    public Task<string?> GetTokenAsync() => SecureStorage.Default.GetAsync(TokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshTokenKey);

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }
}
