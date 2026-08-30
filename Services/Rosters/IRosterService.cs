namespace Servus.Mobile.Services.Rosters;

public interface IRosterService
{
    Task<IReadOnlyList<RosterDto>> GetMyRostersAsync(CancellationToken cancellationToken = default);

    Task<RosterDto> GetRosterByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateRosterAsync(string name, CancellationToken cancellationToken = default);

    Task UpdateRosterNameAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task DeleteRosterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubrosterDto>> GetSubrostersAsync(Guid rosterId, CancellationToken cancellationToken = default);

    Task<SubrosterDto> GetSubrosterByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Null/empty serviceShifts auto-generates the default set (six 4-hour shifts).</summary>
    Task<Guid> CreateSubrosterAsync(Guid rosterId, string name, int minIntervalDays, bool requiresAdminApproval, bool creatorIsMember, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default);

    /// <summary>Null serviceShifts leaves the existing shift set untouched.</summary>
    Task UpdateSubrosterAsync(Guid id, string name, int minIntervalDays, bool requiresAdminApproval, int minDaysAfterReturnBeforeEligible, bool fillShiftsEarliestFirst, TimeOnly serviceDayStartTime, IReadOnlyList<ServiceShiftInputDto>? serviceShifts, CancellationToken cancellationToken = default);

    Task DeleteSubrosterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubrosterScheduleDto> GetScheduleAsync(Guid subrosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<SubrosterScheduleDto> GenerateScheduleAsync(Guid subrosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemberDutyCountDto>> GetDutyCountsAsync(Guid subrosterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubrosterMemberDto>> GetSubrosterMembersAsync(Guid subrosterId, CancellationToken cancellationToken = default);

    Task<Guid> AddSubrosterMemberAsync(Guid subrosterId, Guid memberId, int rank, CancellationToken cancellationToken = default);

    Task RemoveSubrosterMemberAsync(Guid subrosterMemberId, CancellationToken cancellationToken = default);

    Task ReorderSubrosterMemberAsync(Guid subrosterMemberId, int newRank, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubrosterAccessDto>> GetSubrosterAccessesAsync(Guid subrosterId, CancellationToken cancellationToken = default);

    Task<Guid> GrantSubrosterAccessAsync(Guid subrosterId, Guid memberId, bool isFullAdmin, CancellationToken cancellationToken = default);

    Task RevokeSubrosterAccessAsync(Guid accessId, CancellationToken cancellationToken = default);

    Task<SwapDutyAssignmentResultDto> SwapDutyAssignmentAsync(Guid dutyAssignmentId, Guid newMemberId, CancellationToken cancellationToken = default);

    Task MoveDutyAssignmentToShiftAsync(Guid dutyAssignmentId, Guid newServiceShiftId, CancellationToken cancellationToken = default);

    Task<Guid?> GetMyMemberIdAsync(CancellationToken cancellationToken = default);
}
