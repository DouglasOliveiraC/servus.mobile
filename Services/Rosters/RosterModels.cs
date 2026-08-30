namespace Servus.Mobile.Services.Rosters;

public record RosterDto(Guid Id, string Name, DateTime CreatedAt, bool IsAdmin);

public record SubrosterDto(
    Guid Id,
    Guid RosterId,
    string Name,
    int MinIntervalDays,
    int WorkdayShiftSize,
    int RestDayShiftSize,
    bool RequiresAdminApproval,
    int MinDaysAfterReturnBeforeEligible,
    bool FillShiftsEarliestFirst,
    TimeOnly ServiceDayStartTime,
    IReadOnlyList<ServiceShiftDto> ServiceShifts,
    DateTime CreatedAt);

public record ServiceShiftDto(Guid Id, TimeOnly StartTime, TimeOnly EndTime, int WorkdayCapacity, int RestDayCapacity, int SortOrder);

public record ServiceShiftInputDto(TimeOnly StartTime, TimeOnly EndTime, int WorkdayCapacity, int RestDayCapacity);

public record ScheduleAssignmentDto(Guid DutyAssignmentId, Guid MemberId, string MemberName, Guid ServiceShiftId, TimeOnly ShiftStartTime, TimeOnly ShiftEndTime);

public record ScheduleDayDto(DateOnly Date, bool IsRestDay, int RequiredShiftSize, IReadOnlyList<ScheduleAssignmentDto> Assignments);

public record SubrosterScheduleDto(Guid SubrosterId, string SubrosterName, IReadOnlyList<ScheduleDayDto> Days);

public record MemberDto(Guid Id, string Name, string? Email, string? Phone, Guid? UserId, DateTime CreatedAt);

public record SubrosterMemberDto(Guid Id, Guid SubrosterId, Guid MemberId, string MemberName, int Position, string? PendingReturnPriorityReason);

public record MemberDutyCountDto(Guid MemberId, string MemberName, int Position, int AssignmentCount);

public record SubrosterAccessDto(Guid Id, Guid SubrosterId, Guid MemberId, bool IsFullAdmin, DateTime CreatedAt);

public record SwapDutyAssignmentResultDto(Guid DutyAssignmentId, Guid SubrosterId, Guid MemberId, DateOnly Date, bool MinIntervalWarning, string? MinIntervalWarningMessage);
