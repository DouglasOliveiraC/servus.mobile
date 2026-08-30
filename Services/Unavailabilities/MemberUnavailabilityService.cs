using System.Net.Http.Json;

namespace Servus.Mobile.Services.Unavailabilities;

public class MemberUnavailabilityService : IMemberUnavailabilityService
{
    private readonly HttpClient _httpClient;

    public MemberUnavailabilityService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> CreateAsync(Guid memberId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/member-unavailability", new
        {
            MemberId = memberId,
            StartDate = startDate,
            EndDate = endDate
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task<IReadOnlyList<MemberUnavailabilityDto>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<MemberUnavailabilityDto>>($"api/member-unavailability/member/{memberId}", cancellationToken);
        return result ?? [];
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/member-unavailability/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record CreatedResult(Guid Id);
}
