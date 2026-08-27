using System.Net.Http.Json;

namespace Servus.Mobile.Services.Rosters;

public class RosterService : IRosterService
{
    private readonly HttpClient _httpClient;

    public RosterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RosterDto>> GetMyRostersAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<RosterDto>>("api/roster", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SubrosterDto>> GetSubrostersAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SubrosterDto>>($"api/subroster?rosterId={rosterId}", cancellationToken);
        return result ?? [];
    }

    public async Task<SubrosterScheduleDto> GetScheduleAsync(Guid subrosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<SubrosterScheduleDto>(
            $"api/subroster/{subrosterId}/schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

        return result ?? new SubrosterScheduleDto(subrosterId, string.Empty, []);
    }

    public async Task<Guid?> GetMyMemberIdAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/member/me", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var member = await response.Content.ReadFromJsonAsync<MemberDto>(cancellationToken: cancellationToken);
        return member?.Id;
    }
}
