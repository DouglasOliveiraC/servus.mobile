namespace Servus.Mobile.Services.Auth;

public interface ITokenStore
{
    Task SaveTokenAsync(string token);
    Task<string?> GetTokenAsync();
    Task ClearAsync();
}
