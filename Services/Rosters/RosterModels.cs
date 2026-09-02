namespace Servus.Mobile.Services.Rosters;

public record RosterDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    int MinIntervalDays,
    int WorkdayShiftSize,
    int RestDayShiftSize,
    bool RequiresAdminApproval,
    int MinDaysAfterReturnBeforeEligible,
    bool FillShiftsEarliestFirst,
    TimeOnly ServiceDayStartTime,
    QueueDirection WorkdayQueueDirection,
    QueueDirection WeekendQueueDirection,
    IReadOnlyList<ServiceShiftDto> ServiceShifts,
    DateTime CreatedAt,
    bool CanManage);

public enum QueueDirection
{
    TopToBottom,
    BottomToTop
}

public enum QueueType
{
    Workday,
    Weekend
}

public record ServiceShiftDto(Guid Id, TimeOnly StartTime, TimeOnly EndTime, int WorkdayCapacity, int RestDayCapacity, int SortOrder);

public record ServiceShiftInputDto(TimeOnly StartTime, TimeOnly EndTime, int WorkdayCapacity, int RestDayCapacity);

public record ScheduleAssignmentDto(Guid DutyAssignmentId, Guid MemberId, string MemberName, Guid ServiceShiftId, TimeOnly ShiftStartTime, TimeOnly ShiftEndTime, DutyAssignmentStatus Status);

public enum DutyAssignmentStatus
{
    Draft,
    Published
}

public record ScheduleDayDto(DateOnly Date, bool IsRestDay, int RequiredShiftSize, IReadOnlyList<ScheduleAssignmentDto> Assignments);

public record RosterScheduleDto(Guid RosterId, string RosterName, IReadOnlyList<ScheduleDayDto> Days);

/// <summary>
/// Rank é a posição 1-based do membro na ordem institucional da fila — a mesma para as duas
/// rotações, e a que a tela exibe. Ela não muda porque a escala rodou; só uma reordenação
/// explícita a altera. Position é o contador 0-based por trás dela: ordene e exiba por Rank.
///
/// Os dois motivos de prioridade continuam separados porque a vez pode ter sido perdida numa
/// rotação e não na outra.
/// </summary>
public record RosterMemberDto(
    Guid Id, Guid RosterId, Guid MemberId, string MemberName,
    int Position, int Rank,
    string? PendingReturnPriorityWorkdayReason,
    string? PendingReturnPriorityWeekendReason);

public record MemberDutyCountDto(Guid MemberId, string MemberName, int Rank, int AssignmentCount);

public record RosterAccessDto(Guid Id, Guid RosterId, Guid MemberId, bool IsFullAdmin, DateTime CreatedAt);

public record SwapDutyAssignmentResultDto(Guid DutyAssignmentId, Guid RosterId, Guid MemberId, DateOnly Date, bool MinIntervalWarning, string? MinIntervalWarningMessage);

public record ImportRosterMemberEntryDto(string Name, string? Email, string? Phone);

public record ImportedRosterMemberDto(Guid MemberId, Guid RosterMemberId, string Name, int Rank);

public record ImportRosterMembersResultDto(IReadOnlyList<ImportedRosterMemberDto> Imported, IReadOnlyList<string> SkippedDuplicateNames);

public record PublishRosterScheduleResultDto(int AssignmentsPublished);
