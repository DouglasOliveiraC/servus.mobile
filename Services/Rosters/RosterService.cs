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

    public async Task<RosterDto> GetRosterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<RosterDto>($"api/roster/{id}", cancellationToken);
        return result!;
    }

    public async Task<Guid> CreateRosterAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/roster", new { Name = name }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task UpdateRosterNameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/roster/{id}", new { Name = name }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRosterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/roster/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<SubrosterDto>> GetSubrostersAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SubrosterDto>>($"api/subroster?rosterId={rosterId}", cancellationToken);
        return result ?? [];
    }

    public async Task<SubrosterDto> GetSubrosterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<SubrosterDto>($"api/subroster/{id}", cancellationToken);
        return result!;
    }

    public async Task<Guid> CreateSubrosterAsync(Guid rosterId, string name, int minIntervalDays, bool requiresAdminApproval, bool creatorIsMember, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/subroster", new
        {
            RosterId = rosterId,
            Name = name,
            MinIntervalDays = minIntervalDays,
            RequiresAdminApproval = requiresAdminApproval,
            CreatorIsMember = creatorIsMember,
            MinDaysAfterReturnBeforeEligible = minDaysAfterReturnBeforeEligible,
            FillShiftsEarliestFirst = fillShiftsEarliestFirst,
            ServiceDayStartTime = serviceDayStartTime,
            ServiceShifts = serviceShifts
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task UpdateSubrosterAsync(Guid id, string name, int minIntervalDays, bool requiresAdminApproval, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/subroster/{id}", new
        {
            Name = name,
            MinIntervalDays = minIntervalDays,
            RequiresAdminApproval = requiresAdminApproval,
            MinDaysAfterReturnBeforeEligible = minDaysAfterReturnBeforeEligible,
            FillShiftsEarliestFirst = fillShiftsEarliestFirst,
            ServiceDayStartTime = serviceDayStartTime,
            ServiceShifts = serviceShifts
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteSubrosterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/subroster/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SubrosterScheduleDto> GetScheduleAsync(Guid subrosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<SubrosterScheduleDto>(
            $"api/subroster/{subrosterId}/schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

        return result ?? new SubrosterScheduleDto(subrosterId, string.Empty, []);
    }

    public async Task<SubrosterScheduleDto> GenerateScheduleAsync(Guid subrosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/subroster/{subrosterId}/generate", new { From = from, To = to }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SubrosterScheduleDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<IReadOnlyList<MemberDutyCountDto>> GetDutyCountsAsync(Guid subrosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<MemberDutyCountDto>>($"api/subroster/{subrosterId}/duty-counts", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SubrosterMemberDto>> GetSubrosterMembersAsync(Guid subrosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SubrosterMemberDto>>($"api/subroster/{subrosterId}/members", cancellationToken);
        return result ?? [];
    }

    public async Task<Guid> AddSubrosterMemberAsync(Guid subrosterId, Guid memberId, int rank, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/subroster/{subrosterId}/members", new { MemberId = memberId, Rank = rank }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task RemoveSubrosterMemberAsync(Guid subrosterMemberId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/subroster/members/{subrosterMemberId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReorderSubrosterMemberAsync(Guid subrosterMemberId, int newRank, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/subroster/members/{subrosterMemberId}/position", new { NewRank = newRank }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<SubrosterAccessDto>> GetSubrosterAccessesAsync(Guid subrosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SubrosterAccessDto>>($"api/subroster/{subrosterId}/access", cancellationToken);
        return result ?? [];
    }

    public async Task<Guid> GrantSubrosterAccessAsync(Guid subrosterId, Guid memberId, bool isFullAdmin, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/subroster/{subrosterId}/access", new { MemberId = memberId, IsFullAdmin = isFullAdmin }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task RevokeSubrosterAccessAsync(Guid accessId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/subroster/access/{accessId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SwapDutyAssignmentResultDto> SwapDutyAssignmentAsync(Guid dutyAssignmentId, Guid newMemberId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/duty-assignment/{dutyAssignmentId}/swap", new { NewMemberId = newMemberId }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SwapDutyAssignmentResultDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task MoveDutyAssignmentToShiftAsync(Guid dutyAssignmentId, Guid newServiceShiftId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/duty-assignment/{dutyAssignmentId}/shift", new { NewServiceShiftId = newServiceShiftId }, cancellationToken);
        response.EnsureSuccessStatusCode();
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

    private record CreatedResult(Guid Id);
}
