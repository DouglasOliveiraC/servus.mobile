using System.Net;
using System.Net.Http.Json;
using Servus.Mobile.Services.Http;

namespace Servus.Mobile.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;

    public AuthService(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
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
            if (body is null || string.IsNullOrWhiteSpace(body.Token))
            {
                return AuthResult.Fail("Resposta inválida do servidor.");
            }

            await _tokenStore.SaveTokenAsync(body.Token);
            return AuthResult.Ok();
        }

        var errorMessage = await ExtractErrorMessageAsync(response, cancellationToken);
        return AuthResult.Fail(errorMessage);
    }

    public async Task<AuthResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                new RegisterRequest(name, email, password),
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return AuthResult.Fail("Não foi possível conectar ao servidor. Verifique sua conexão.");
        }

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
            if (body is null || string.IsNullOrWhiteSpace(body.Token))
            {
                return AuthResult.Fail("Resposta inválida do servidor.");
            }

            await _tokenStore.SaveTokenAsync(body.Token);
            return AuthResult.Ok();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return AuthResult.Fail("Cadastro ainda não está disponível nesta versão.");
        }

        var errorMessage = await ExtractErrorMessageAsync(response, cancellationToken);
        return AuthResult.Fail(errorMessage);
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
        return new CurrentUser(userId, email ?? string.Empty);
    }

    public Task LogoutAsync() => _tokenStore.ClearAsync();

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return "Muitas tentativas. Aguarde um momento antes de tentar novamente.";
        }

        var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(cancellationToken: cancellationToken);

        if (problem?.Errors is { Count: > 0 })
        {
            return string.Join(" ", problem.Errors.SelectMany(e => e.Value));
        }

        if (!string.IsNullOrWhiteSpace(problem?.Title))
        {
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? "E-mail ou senha incorretos."
                : problem.Title;
        }

        return "Não foi possível entrar. Tente novamente.";
    }
}
