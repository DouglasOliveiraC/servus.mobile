namespace Servus.Mobile.Services.CalendarOverrides;

public record CalendarDayOverrideDto(Guid Id, Guid OrganizationId, DateOnly Date, bool IsRestDay, DateTime CreatedAt);
