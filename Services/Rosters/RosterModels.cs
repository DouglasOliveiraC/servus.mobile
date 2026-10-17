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
    int MinDaysBeforeLeaveBlocked,
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

public record GenerateScheduleResultDto(int DaysProcessed, int AssignmentsCreated, IReadOnlyList<DateOnly> UnderstaffedDates);

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

/// <summary>
/// Retrato consolidado de UM membro dentro de UMA escala — o que o administrador precisa
/// para decidir sobre essa pessoa sem abrir a escala inteira. Escopado à escala de
/// propósito: serviços cumpridos e posição na fila só significam algo dentro de uma fila.
/// </summary>
public record RosterMemberProfileDto(
    Guid RosterMemberId,
    Guid RosterId,
    string RosterName,
    Guid MemberId,
    string MemberName,
    string? Email,
    string? Phone,
    bool IsLinkedToUser,
    DateTime JoinedQueueAt,
    int Rank,
    int QueueSize,
    bool IsWorkdayNextUp,
    bool IsWeekendNextUp,
    string? PendingReturnPriorityWorkdayReason,
    string? PendingReturnPriorityWeekendReason,
    RosterMemberDutySummaryDto Duties,
    IReadOnlyList<RosterMemberDutyDto> UpcomingDuties,
    IReadOnlyList<RosterMemberDutyDto> RecentDuties,
    IReadOnlyList<RosterMemberSwapDto> SwapHistory,
    IReadOnlyList<RosterMemberUnavailabilityDto> Unavailabilities);

/// <summary>
/// Completed é o que já passou E está publicado. Draft fica à parte porque um rascunho
/// ainda pode sumir numa regeração e não representa compromisso assumido.
/// </summary>
public record RosterMemberDutySummaryDto(
    int Completed,
    int CompletedWorkday,
    int CompletedRestDay,
    int Scheduled,
    int Draft,
    DateOnly? LastDutyDate,
    DateOnly? NextDutyDate,
    double QueueAverageCompleted);

public record RosterMemberDutyDto(
    Guid Id, DateOnly Date, bool IsRestDay,
    TimeOnly ShiftStartTime, TimeOnly ShiftEndTime,
    DutyAssignmentStatus Status,
    IReadOnlyList<string> CompanionNames);

/// <summary>Direção vista do lado do membro exibido: Out = alguém assumiu o dia dele.</summary>
public record RosterMemberSwapDto(
    Guid Id, DateOnly Date, SwapDirection Direction,
    Guid CounterpartMemberId, string CounterpartMemberName,
    SwapCause Cause, DateTime CreatedAt);

public enum SwapDirection
{
    Out,
    In
}

/// <summary>
/// Espelha Servus.Domain.Enums.SwapCause. Existe separado de SwapCauseDto
/// (Services/SwapHistory) porque aquele não corresponde ao enum do servidor: a API serializa
/// enum como inteiro, e ali 0/1 são lidos como Manual/Approved em vez de PeerSwap/AdminSwap.
/// </summary>
public enum SwapCause
{
    PeerSwap,
    AdminSwap
}

public record RosterMemberUnavailabilityDto(
    Guid Id, DateOnly StartDate, DateOnly EndDate,
    string? ReasonName, Unavailabilities.MemberUnavailabilityStatus Status,
    int DayCount, bool IsCurrent, DateTime CreatedAt);

public record RosterAccessDto(Guid Id, Guid RosterId, Guid MemberId, bool IsFullAdmin, DateTime CreatedAt);

public record SwapDutyAssignmentResultDto(Guid DutyAssignmentId, Guid RosterId, Guid MemberId, DateOnly Date, bool MinIntervalWarning, string? MinIntervalWarningMessage);

public record ImportRosterMemberEntryDto(string Name, string? Email, string? Phone);

public record ImportedRosterMemberDto(Guid MemberId, Guid RosterMemberId, string Name, int Rank);

public record ImportRosterMembersResultDto(IReadOnlyList<ImportedRosterMemberDto> Imported, IReadOnlyList<string> SkippedDuplicateNames);

public record PublishRosterScheduleResultDto(int AssignmentsPublished);
