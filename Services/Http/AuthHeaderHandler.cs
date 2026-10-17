using System.Net;
using System.Net.Http.Headers;
using Servus.Mobile.Services.Auth;

namespace Servus.Mobile.Services.Http;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ISessionManager _sessionManager;

    public AuthHeaderHandler(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffered up front so the body can still be copied into the retry after the first send.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
        }

        var token = await _sessionManager.GetValidAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (token is null || !IsTokenRejection(response))
        {
            return response;
        }

        var newToken = await _sessionManager.RefreshAsync(token, cancellationToken);
        if (newToken is null)
        {
            return response;
        }

        response.Dispose();
        var retry = await CloneAsync(request, cancellationToken);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    // Only the JwtBearer challenge carries WWW-Authenticate: Bearer. A 401 from a handler (e.g.
    // wrong current password on change-password) has none, and must reach the caller as-is
    // instead of costing a refresh.
    private static bool IsTokenRejection(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.Unauthorized
        && response.Headers.WwwAuthenticate.Any(h => string.Equals(h.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase));

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
