using System.Net;
using System.Net.Http.Json;
using Servus.Mobile.Services.Push;
using Servus.Mobile.Services.Realtime;

namespace Servus.Mobile.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;
    private readonly IRealtimeClient _realtimeClient;
    private readonly ISessionManager _sessionManager;
    private readonly IPushRegistrationService _pushRegistration;

    public AuthService(HttpClient httpClient, ITokenStore tokenStore, IRealtimeClient realtimeClient, ISessionManager sessionManager, IPushRegistrationService pushRegistration)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _realtimeClient = realtimeClient;
        _sessionManager = sessionManager;
        _pushRegistration = pushRegistration;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest(email, password),
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return AuthResult.Fail("Não foi possível conectar ao servidor. Verifique sua conexão.");
        }

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
            if (body is null || string.IsNullOrWhiteSpace(body.Token) || string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                return AuthResult.Fail("Resposta inválida do servidor.");
            }

            await _tokenStore.SaveTokensAsync(body.Token, body.RefreshToken);
            await _realtimeClient.ConnectAsync();
            return AuthResult.Ok();
        }

        var errorMessage = await ExtractErrorMessageAsync(response, cancellationToken);
        return AuthResult.Fail(errorMessage);
    }

    public async Task<AuthResult> LoginAsDemoAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("api/auth/demo", content: null, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return AuthResult.Fail("Não foi possível conectar ao servidor. Verifique sua conexão.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return AuthResult.Fail(response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "Você já abriu algumas demonstrações há pouco. Tente de novo mais tarde.",
                HttpStatusCode.ServiceUnavailable => "A demonstração está indisponível no momento. Tente de novo mais tarde.",
                _ => "Não foi possível abrir a demonstração. Tente novamente."
            });
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        if (body is null || string.IsNullOrWhiteSpace(body.Token) || string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            return AuthResult.Fail("Resposta inválida do servidor.");
        }

        await _tokenStore.SaveTokensAsync(body.Token, body.RefreshToken);
        await _realtimeClient.ConnectAsync();
        return AuthResult.Ok();
    }

    public async Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                new RegisterRequest(email, name, password),
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return AuthResult.Fail("Não foi possível conectar ao servidor. Verifique sua conexão.");
        }

        if (response.IsSuccessStatusCode)
        {
            // Registering does not sign in: the API answers 202 without tokens, the same for a
            // new and an already registered e-mail. The account is only usable after the e-mail
            // confirmation.
            return AuthResult.Ok();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return AuthResult.Fail("Cadastro ainda não está disponível nesta versão.");
        }

        var errorMessage = await ExtractErrorMessageAsync(response, cancellationToken);
        return AuthResult.Fail(errorMessage);
    }

    public async Task<AuthResult> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", new { Email = email }, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return AuthResult.Fail("Não foi possível conectar ao servidor. Verifique sua conexão.");
        }

        // The API answers the same for known and unknown addresses, so success says nothing
        // about whether the account exists.
        if (response.IsSuccessStatusCode)
        {
            return AuthResult.Ok();
        }

        return AuthResult.Fail(response.StatusCode == HttpStatusCode.TooManyRequests
            ? "Muitas tentativas. Aguarde um momento antes de tentar novamente."
            : "Não foi possível enviar o link agora. Tente novamente.");
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var token = await _sessionManager.GetValidAccessTokenAsync(cancellationToken);
        if (token is null)
        {
            return false;
        }

        await _realtimeClient.ConnectAsync();
        return true;
    }

    public async Task<CurrentUser?> GetCurrentUserAsync()
    {
        var token = await _tokenStore.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var claims = JwtPayload.Parse(token);
        if (!claims.TryGetValue("sub", out var sub) || !Guid.TryParse(sub, out var userId))
        {
            return null;
        }

        claims.TryGetValue("email", out var email);

        var isDemo = claims.TryGetValue("servus_demo", out var demo) && demo == "true";
        DateTime? demoExpiresAt = claims.TryGetValue("servus_demo_exp", out var demoExp) && long.TryParse(demoExp, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;

        return new CurrentUser(userId, email ?? string.Empty, isDemo, demoExpiresAt);
    }

    public async Task WarmUpAsync()
    {
        try
        {
            using var _ = await _httpClient.GetAsync("health/warm");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
        }
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await _tokenStore.GetRefreshTokenAsync();
        await _pushRegistration.UnregisterAsync();
        await _realtimeClient.DisconnectAsync();
        await _tokenStore.ClearAsync();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        // Best effort: the local session is already gone, so a failure here only leaves the
        // refresh token alive server-side until it expires.
        try
        {
            using var _ = await _httpClient.PostAsJsonAsync("api/auth/logout", new { RefreshToken = refreshToken });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
        }
    }

    // Never shows problem.Errors/Title from the API: they are ASP.NET model validation messages
    // in English with raw field names (e.g. "The DisplayName field is required."), so each known
    // status maps to a fixed Portuguese message.
    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var isBlocked = response.StatusCode == HttpStatusCode.Forbidden && await IsBlockedAsync(response, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => "Muitas tentativas. Aguarde um momento antes de tentar novamente.",
            // A locked-out account gets the same 401 as a wrong password (the API never says
            // which), so the hint has to cover both.
            HttpStatusCode.Unauthorized => "E-mail ou senha incorretos. Se você errou a senha várias vezes, aguarde alguns minutos ou redefina sua senha.",
            HttpStatusCode.BadRequest => "Verifique os dados informados e tente novamente.",
            // 403 is either an unconfirmed e-mail or a blocked account; only the problem title
            // tells them apart. Registration no longer reports "e-mail already registered".
            HttpStatusCode.Forbidden when isBlocked => "Sua conta está bloqueada. Fale com o suporte.",
            HttpStatusCode.Forbidden => "Confirme seu e-mail antes de entrar. Verifique sua caixa de entrada.",
            _ => "Não foi possível concluir. Tente novamente."
        };
    }

    // Matches UserBlockedException's message ("This account has been blocked.") in the title.
    private static async Task<bool> IsBlockedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body.Contains("blocked", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
