using System.Text;
using System.Text.Json;

namespace Servus.Mobile.Services.Auth;

/// <summary>
/// Decodifica apenas o payload do JWT (sem validar assinatura — isso já foi
/// feito pela API). Usado no cliente só para ler claims como sub/email e
/// popular a UI, nunca para decisão de autorização.
/// </summary>
public static class JwtPayload
{
    public static IReadOnlyDictionary<string, string> Parse(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return new Dictionary<string, string>();
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var document = JsonDocument.Parse(payloadJson);

        var claims = new Dictionary<string, string>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            claims[property.Name] = property.Value.ToString();
        }

        return claims;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
