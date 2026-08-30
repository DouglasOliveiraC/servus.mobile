namespace Servus.Mobile.Services.Users;

public record UserDto(Guid Id, string Email, string DisplayName, DateTime CreatedAt);
