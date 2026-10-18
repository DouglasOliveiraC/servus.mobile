namespace Servus.Mobile.Services.Auth;

public record CurrentUser(Guid Id, string Email, bool IsDemo = false, DateTime? DemoExpiresAt = null);
