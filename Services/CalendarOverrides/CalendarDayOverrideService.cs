using System.Net.Http.Json;

namespace Servus.Mobile.Services.CalendarOverrides;

public class CalendarDayOverrideService : ICalendarDayOverrideService
{
    private readonly HttpClient _httpClient;

    public CalendarDayOverrideService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> CreateAsync(Guid organizationId, DateOnly date, bool isRestDay, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/calendar-day-override", new
        {
            OrganizationId = organizationId,
            Date = date,
            IsRestDay = isRestDay
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task UpdateAsync(Guid id, bool isRestDay, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/calendar-day-override/{id}", new { IsRestDay = isRestDay }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/calendar-day-override/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<CalendarDayOverrideDto>> GetByRangeAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<CalendarDayOverrideDto>>(
            $"api/calendar-day-override?organizationId={organizationId}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", cancellationToken);
        return result ?? [];
    }

    private record CreatedResult(Guid Id);
}
