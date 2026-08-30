namespace Servus.Mobile.Features.Home;

public record RosterSummary(Guid Id, string Name, int SubrosterCount, int UpcomingAssignmentCount);

public record UpcomingDuty(string RosterName, string SubrosterName, DateOnly Date, IReadOnlyList<string> MemberNames, bool IsMine);

/// <summary>
/// One row per day (not per day+subroster) for the home screen — PreviewNames is
/// intentionally capped so the card stays a summary; DayDetailPage re-fetches the
/// full, untruncated breakdown for that date on demand.
/// </summary>
public record DailyDutySummary(DateOnly Date, int TotalAssignments, int SubrosterCount, bool HasMine, IReadOnlyList<string> PreviewNames);

public record MemberLoadSlice(string MemberName, int AssignmentCount);

public record SwapTrendPoint(string PeriodLabel, int Count);

public record CoverageDayPoint(DateOnly Date, string DayLabel, string WeekdayLabel, int Assigned, int Required)
{
    public bool HasGap => Required > 0 && Assigned < Required;

    public string StatusColor => Required == 0
        ? "#E3E6E5"
        : HasGap
            ? "#D98C3B"
            : "#3D6E68"; // Success (verde, independente do BrandPrimary)
}

public record NextDutyInfo(string RosterName, string SubrosterName, DateOnly Date, int DaysUntil);
