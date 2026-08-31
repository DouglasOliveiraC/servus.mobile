using System.Net.Http.Json;
using Servus.Mobile.Services.Members;

namespace Servus.Mobile.Services.Rosters;

public class RosterService : IRosterService
{
    private readonly HttpClient _httpClient;

    public RosterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RosterDto>> GetRostersAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<RosterDto>>($"api/roster?organizationId={organizationId}", cancellationToken);
        return result ?? [];
    }

    public async Task<RosterDto> GetRosterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<RosterDto>($"api/roster/{id}", cancellationToken);
        return result!;
    }

    public async Task<Guid> CreateRosterAsync(Guid organizationId, string name, int minIntervalDays, bool requiresAdminApproval, bool creatorIsMember, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, QueueDirection workdayQueueDirection, QueueDirection weekendQueueDirection, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/roster", new
        {
            OrganizationId = organizationId,
            Name = name,
            MinIntervalDays = minIntervalDays,
            RequiresAdminApproval = requiresAdminApproval,
            CreatorIsMember = creatorIsMember,
            MinDaysAfterReturnBeforeEligible = minDaysAfterReturnBeforeEligible,
            FillShiftsEarliestFirst = fillShiftsEarliestFirst,
            ServiceDayStartTime = serviceDayStartTime,
            WorkdayQueueDirection = workdayQueueDirection,
            WeekendQueueDirection = weekendQueueDirection,
            ServiceShifts = serviceShifts
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task UpdateRosterAsync(Guid id, string name, int minIntervalDays, bool requiresAdminApproval, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, QueueDirection workdayQueueDirection, QueueDirection weekendQueueDirection, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/roster/{id}", new
        {
            Name = name,
            MinIntervalDays = minIntervalDays,
            RequiresAdminApproval = requiresAdminApproval,
            MinDaysAfterReturnBeforeEligible = minDaysAfterReturnBeforeEligible,
            FillShiftsEarliestFirst = fillShiftsEarliestFirst,
            ServiceDayStartTime = serviceDayStartTime,
            WorkdayQueueDirection = workdayQueueDirection,
            WeekendQueueDirection = weekendQueueDirection,
            ServiceShifts = serviceShifts
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRosterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/roster/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<RosterScheduleDto> GetScheduleAsync(Guid rosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<RosterScheduleDto>(
            $"api/roster/{rosterId}/schedule?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

        return result ?? new RosterScheduleDto(rosterId, string.Empty, []);
    }

    public async Task<RosterScheduleDto> GenerateScheduleAsync(Guid rosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/roster/{rosterId}/generate", new { From = from, To = to }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RosterScheduleDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<IReadOnlyList<MemberDutyCountDto>> GetDutyCountsAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<MemberDutyCountDto>>($"api/roster/{rosterId}/duty-counts", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<RosterMemberDto>> GetRosterMembersAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<RosterMemberDto>>($"api/roster/{rosterId}/members", cancellationToken);
        return result ?? [];
    }

    public async Task<Guid> AddRosterMemberAsync(Guid rosterId, Guid memberId, int rank, QueueType queueType, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/roster/{rosterId}/members", new { MemberId = memberId, Rank = rank, QueueType = queueType }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task RemoveRosterMemberAsync(Guid rosterMemberId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/roster/members/{rosterMemberId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReorderRosterMemberAsync(Guid rosterMemberId, int newRank, QueueType queueType, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/roster/members/{rosterMemberId}/position", new { NewRank = newRank, QueueType = queueType }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<RosterAccessDto>> GetRosterAccessesAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<RosterAccessDto>>($"api/roster/{rosterId}/access", cancellationToken);
        return result ?? [];
    }

    public async Task<Guid> GrantRosterAccessAsync(Guid rosterId, Guid memberId, bool isFullAdmin, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/roster/{rosterId}/access", new { MemberId = memberId, IsFullAdmin = isFullAdmin }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task RevokeRosterAccessAsync(Guid accessId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/roster/access/{accessId}", cancellationToken);
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
