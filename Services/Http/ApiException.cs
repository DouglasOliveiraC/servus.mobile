using System.Net;
using System.Net.Http.Json;

namespace Servus.Mobile.Services.Http;

/// <summary>
/// Derives from HttpRequestException so existing <c>catch (HttpRequestException ex)</c> blocks
/// that switch on StatusCode keep working; Title adds the problem title for the cases where
/// one status covers several causes (e.g. the 409s of claim code redemption).
/// </summary>
public class ApiException : HttpRequestException
{
    public ApiException(HttpStatusCode statusCode, string? title)
        : base($"API answered {(int)statusCode}: {title}", null, statusCode)
    {
        Title = title;
    }

    // The API's English exception message, for matching only — never shown to the user.
    public string? Title { get; }

    public bool TitleContains(string fragment) =>
        Title?.Contains(fragment, StringComparison.OrdinalIgnoreCase) == true;
}

public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessWithProblemAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? title = null;
        try
        {
            title = (await response.Content.ReadFromJsonAsync<ApiProblemDetails>(cancellationToken: cancellationToken))?.Title;
        }
        catch (Exception)
        {
            // Not every error body is a problem document (e.g. a proxy's HTML page).
        }

        throw new ApiException(response.StatusCode, title);
    }
}
