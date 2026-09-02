namespace Servus.Mobile.Services.Rosters;

public interface IRosterService
{
    Task<IReadOnlyList<RosterDto>> GetRostersAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<RosterDto> GetRosterByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Null/empty serviceShifts auto-generates the default set (six 4-hour shifts).</summary>
    Task<Guid> CreateRosterAsync(Guid organizationId, string name, int minIntervalDays, bool requiresAdminApproval, bool creatorIsMember, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, QueueDirection workdayQueueDirection, QueueDirection weekendQueueDirection, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default);

    /// <summary>Null serviceShifts leaves the existing shift set untouched.</summary>
    Task UpdateRosterAsync(Guid id, string name, int minIntervalDays, bool requiresAdminApproval, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, QueueDirection workdayQueueDirection, QueueDirection weekendQueueDirection, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default);

    Task DeleteRosterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RosterScheduleDto> GetScheduleAsync(Guid rosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<RosterScheduleDto> GenerateScheduleAsync(Guid rosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemberDutyCountDto>> GetDutyCountsAsync(Guid rosterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RosterMemberDto>> GetRosterMembersAsync(Guid rosterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rank é a posição 1-based na ordem institucional da fila — uma só, percorrida pelas
    /// duas rotações —, então não há fila a escolher.
    /// </summary>
    Task<Guid> AddRosterMemberAsync(Guid rosterId, Guid memberId, int rank, CancellationToken cancellationToken = default);

    Task RemoveRosterMemberAsync(Guid rosterMemberId, CancellationToken cancellationToken = default);

    Task ReorderRosterMemberAsync(Guid rosterMemberId, int newRank, CancellationToken cancellationToken = default);

    Task<ImportRosterMembersResultDto> ImportRosterMembersAsync(Guid rosterId, IReadOnlyList<ImportRosterMemberEntryDto> entries, CancellationToken cancellationToken = default);

    /// <summary>Null leaves that rotation's starting point untouched.</summary>
    Task SetNextUpRosterMemberAsync(Guid rosterId, Guid? workdayRosterMemberId, Guid? weekendRosterMemberId, CancellationToken cancellationToken = default);

    Task<PublishRosterScheduleResultDto> PublishScheduleAsync(Guid rosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RosterAccessDto>> GetRosterAccessesAsync(Guid rosterId, CancellationToken cancellationToken = default);

    Task<Guid> GrantRosterAccessAsync(Guid rosterId, Guid memberId, bool isFullAdmin, CancellationToken cancellationToken = default);

    Task RevokeRosterAccessAsync(Guid accessId, CancellationToken cancellationToken = default);

    Task<SwapDutyAssignmentResultDto> SwapDutyAssignmentAsync(Guid dutyAssignmentId, Guid newMemberId, CancellationToken cancellationToken = default);

    Task MoveDutyAssignmentToShiftAsync(Guid dutyAssignmentId, Guid newServiceShiftId, CancellationToken cancellationToken = default);

    Task<Guid?> GetMyMemberIdAsync(CancellationToken cancellationToken = default);
}
