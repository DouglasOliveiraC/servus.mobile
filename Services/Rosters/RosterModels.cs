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
    DateTime CreatedAt);

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

public record RosterMemberDto(
    Guid Id, Guid RosterId, Guid MemberId, string MemberName,
    int WorkdayPosition, int WeekendPosition,
    string? PendingReturnPriorityWorkdayReason,
    string? PendingReturnPriorityWeekendReason);

public record MemberDutyCountDto(Guid MemberId, string MemberName, int Position, int AssignmentCount);

public record RosterAccessDto(Guid Id, Guid RosterId, Guid MemberId, bool IsFullAdmin, DateTime CreatedAt);

public record SwapDutyAssignmentResultDto(Guid DutyAssignmentId, Guid RosterId, Guid MemberId, DateOnly Date, bool MinIntervalWarning, string? MinIntervalWarningMessage);
