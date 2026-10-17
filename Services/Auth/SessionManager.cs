using System.Net.Http.Json;

namespace Servus.Mobile.Services.Auth;

public class SessionManager : ISessionManager
{
    public const string HttpClientName = "auth-refresh";

    // Refreshing a bit before the real expiry avoids sending a token that dies in flight.
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenStore _tokenStore;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public SessionManager(IHttpClientFactory httpClientFactory, ITokenStore tokenStore)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore;
    }

    public event EventHandler? SessionExpired;

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await _tokenStore.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return IsExpiring(token) ? await RefreshAsync(token, cancellationToken) : token;
    }

    public async Task<string?> RefreshAsync(string? rejectedToken, CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var current = await _tokenStore.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(current) && current != rejectedToken && !IsExpiring(current))
            {
                return current;
            }

            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            HttpResponseMessage response;
            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);
                response = await client.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken }, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Offline is not an expired session: keep the tokens so the next request can
                // still refresh once the network is back.
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode is >= 400 and < 500 and not 429)
                {
                    await _tokenStore.ClearAsync();
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                }

                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
            if (body is null || string.IsNullOrWhiteSpace(body.Token) || string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                return null;
            }

            await _tokenStore.SaveTokensAsync(body.Token, body.RefreshToken);
            return body.Token;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static bool IsExpiring(string token)
    {
        var claims = JwtPayload.Parse(token);
        if (!claims.TryGetValue("exp", out var exp) || !long.TryParse(exp, out var expSeconds))
        {
            return false;
        }

        return DateTimeOffset.FromUnixTimeSeconds(expSeconds) - ExpiryMargin <= DateTimeOffset.UtcNow;
    }
}
