namespace Servus.Mobile.Services.CalendarOverrides;

public interface ICalendarDayOverrideService
{
    Task<Guid> CreateAsync(Guid rosterId, DateOnly date, bool isRestDay, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, bool isRestDay, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarDayOverrideDto>> GetByRangeAsync(Guid rosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
