using System.Net;
using System.Net.Http.Json;
using Servus.Mobile.Services.Realtime;

namespace Servus.Mobile.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;
    private readonly IRealtimeClient _realtimeClient;

    public AuthService(HttpClient httpClient, ITokenStore tokenStore, IRealtimeClient realtimeClient)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _realtimeClient = realtimeClient;
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
            await _realtimeClient.ConnectAsync();
            return AuthResult.Ok();
        }

        var errorMessage = ExtractErrorMessage(response);
        return AuthResult.Fail(errorMessage);
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
            // Registro não autentica: a API responde 202 sem tokens, igual para e-mail novo e
            // já cadastrado. A conta só fica utilizável após a confirmação por e-mail.
            return AuthResult.Ok();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return AuthResult.Fail("Cadastro ainda não está disponível nesta versão.");
        }

        var errorMessage = ExtractErrorMessage(response);
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

    public async Task LogoutAsync()
    {
        await _realtimeClient.DisconnectAsync();
        await _tokenStore.ClearAsync();
    }

    // Nunca repassa problem.Errors/Title da API ao usuário: são mensagens de model validation
    // do ASP.NET em inglês com nomes de campo crus (ex: "The DisplayName field is required."),
    // então cada status conhecido mapeia para uma mensagem fixa em português.
    private static string ExtractErrorMessage(HttpResponseMessage response) => response.StatusCode switch
    {
        HttpStatusCode.TooManyRequests => "Muitas tentativas. Aguarde um momento antes de tentar novamente.",
        HttpStatusCode.Unauthorized => "E-mail ou senha incorretos.",
        HttpStatusCode.BadRequest => "Verifique os dados informados e tente novamente.",
        // O cadastro não devolve mais "e-mail já cadastrado" em nenhum status: a API responde
        // igual para e-mail novo e existente para não revelar quem tem conta.
        HttpStatusCode.Forbidden => "Confirme seu e-mail antes de entrar. Verifique sua caixa de entrada.",
        _ => "Não foi possível concluir. Tente novamente."
    };
}
