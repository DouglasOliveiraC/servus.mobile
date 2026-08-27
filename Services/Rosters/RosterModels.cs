namespace Servus.Mobile.Services.Rosters;

public record RosterDto(Guid Id, string Name, DateTime CreatedAt);

public record SubrosterDto(
    Guid Id,
    Guid RosterId,
    string Name,
    int MinIntervalDays,
    int WorkdayShiftSize,
    int RestDayShiftSize,
    bool RequiresAdminApproval,
    DateTime CreatedAt);

public record ScheduleAssignmentDto(Guid DutyAssignmentId, Guid MemberId, string MemberName);

public record ScheduleDayDto(DateOnly Date, bool IsRestDay, int RequiredShiftSize, IReadOnlyList<ScheduleAssignmentDto> Assignments);

public record SubrosterScheduleDto(Guid SubrosterId, string SubrosterName, IReadOnlyList<ScheduleDayDto> Days);

public record MemberDto(Guid Id, string Name, Guid? UserId, DateTime CreatedAt);
