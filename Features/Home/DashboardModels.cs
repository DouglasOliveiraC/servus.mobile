namespace Servus.Mobile.Features.Home;

/// <summary>
/// One row per day (not per day+roster) for the home screen — PreviewNames is
/// intentionally capped so the card stays a summary; DayDetailPage re-fetches the
/// full, untruncated breakdown for that date on demand.
/// </summary>
public record DailyDutySummary(DateOnly Date, int TotalAssignments, int RosterCount, bool HasMine, IReadOnlyList<string> PreviewNames)
{
    public string DayNumber => DateLabels.DayNumber(Date);

    public string WeekdayShort => DateLabels.WeekdayShort(Date);

    public string Headline =>
        (TotalAssignments == 1 ? "1 plantão" : $"{TotalAssignments} plantões") + " · " +
        (RosterCount == 1 ? "1 escala" : $"{RosterCount} escalas");
}

// Color is carried per slice so the custom legend matches the doughnut's PaletteBrushes order.
public record MemberLoadSlice(string MemberName, int AssignmentCount, Color Color);

public record SwapTrendPoint(string PeriodLabel, int Count);

public record CoverageDayPoint(DateOnly Date, string DayLabel, string WeekdayLabel, int Assigned, int Required)
{
    public bool HasGap => Required > 0 && Assigned < Required;

    // Grey only when the roster requires nobody that day (full day-off override);
    // Assigned > 0 with Required == 0 still means the day is covered on top of that.
    public string StatusColor => Required == 0
        ? (Assigned > 0 ? "#3D6E68" : "#E3E6E5")
        : HasGap
            ? "#D98C3B"
            : "#3D6E68"; // Success (green, independent of BrandPrimary)
}

public record MyShift(
    DateOnly Date,
    string RosterName,
    string OrganizationName,
    TimeOnly ShiftStart,
    TimeOnly ShiftEnd,
    bool IsRestDay,
    IReadOnlyList<string> CompanionNames)
{
    public string TimeRange => $"{ShiftStart:HH\\:mm}–{ShiftEnd:HH\\:mm}";

    public string DayNumber => DateLabels.DayNumber(Date);

    public string WeekdayShort => DateLabels.WeekdayShort(Date);

    public bool HasCompanions => CompanionNames.Count > 0;

    public string CompanionsText => "com " + string.Join(", ", CompanionNames);
}

public record OnDutyEntry(string MemberName, string RosterName, TimeOnly ShiftStart, TimeOnly ShiftEnd)
{
    public string Initials
    {
        get
        {
            var parts = MemberName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : (parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : "?");
        }
    }

    // 00:00–23:59 is how a whole-day shift is stored; repeating it on every row said nothing.
    public string Subtitle => ShiftStart == TimeOnly.MinValue && ShiftEnd >= new TimeOnly(23, 59)
        ? $"{RosterName} · dia inteiro"
        : $"{RosterName} · {ShiftStart:HH\\:mm}–{ShiftEnd:HH\\:mm}";
}

public record MyRosterSummary(Guid RosterId, string RosterName, string OrganizationName);

public record HomeShortcut(string Title, string Subtitle, Microsoft.Maui.Controls.Shapes.Geometry Icon, string Route);

// The app sets no culture, so "ddd" in a XAML StringFormat follows the device language
// (English on the emulator); pinned to pt-BR here like the rest of the UI text.
internal static class DateLabels
{
    private static readonly System.Globalization.CultureInfo PtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

    public static string DayNumber(DateOnly date) => date.ToString("dd", PtBr);

    public static string WeekdayShort(DateOnly date) => date.ToString("ddd", PtBr).TrimEnd('.');
}
