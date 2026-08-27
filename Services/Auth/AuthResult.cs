namespace Servus.Mobile.Services.Auth;

public class AuthResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }

    private AuthResult(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public static AuthResult Ok() => new(true, null);
    public static AuthResult Fail(string message) => new(false, message);
}
