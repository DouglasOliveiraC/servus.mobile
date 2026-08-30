namespace Servus.Mobile.Services.CalendarOverrides;

public record CalendarDayOverrideDto(Guid Id, Guid RosterId, DateOnly Date, bool IsRestDay, DateTime CreatedAt);
