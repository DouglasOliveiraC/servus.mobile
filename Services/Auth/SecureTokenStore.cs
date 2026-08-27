namespace Servus.Mobile.Services.Auth;

public class SecureTokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";

    public Task SaveTokenAsync(string token) => SecureStorage.Default.SetAsync(TokenKey, token);

    public Task<string?> GetTokenAsync() => SecureStorage.Default.GetAsync(TokenKey);

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        return Task.CompletedTask;
    }
}
