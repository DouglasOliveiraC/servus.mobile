namespace Servus.Mobile.Services.Organizations;

public record OrganizationDto(Guid Id, string Name, DateTime CreatedAt, bool IsAdmin);
