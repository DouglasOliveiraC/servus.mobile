using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;
using Servus.Mobile.Services.Unavailabilities;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(RosterId), "rosterId")]
[QueryProperty(nameof(MemberId), "memberId")]
public partial class RosterMemberProfileViewModel : BaseViewModel
{
    private readonly IRosterService _rosterService;

    public RosterMemberProfileViewModel(
        IRosterService rosterService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial Guid RosterId { get; set; }

    [ObservableProperty]
    public partial Guid MemberId { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string MemberName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RosterName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string QueueStanding { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Contact { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JoinedQueueAt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLinkedToUser { get; set; }

    [ObservableProperty]
    public partial string CompletedCount { get; set; } = "0";

    [ObservableProperty]
    public partial string CompletedComparison { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WorkdayCount { get; set; } = "0";

    [ObservableProperty]
    public partial string RestDayCount { get; set; } = "0";

    [ObservableProperty]
    public partial string NextDuty { get; set; } = "—";

    [ObservableProperty]
    public partial string NextDutyHint { get; set; } = string.Empty;

    /// <summary>
    /// Os dois motivos vêm por rotação (a vez pode ter sido perdida num dia útil e não num
    /// fim de semana) e são unidos numa linha só porque a fila também é uma só.
    /// </summary>
    [ObservableProperty]
    public partial string? ReturnPriorityReason { get; set; }

    [ObservableProperty]
    public partial bool HasReturnPriority { get; set; }

    [ObservableProperty]
    public partial string NextUpBadges { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasNextUpBadges { get; set; }

    public ObservableCollection<DutyRow> UpcomingDuties { get; } = [];

    public ObservableCollection<DutyRow> RecentDuties { get; } = [];

    public ObservableCollection<SwapRow> Swaps { get; } = [];

    public ObservableCollection<UnavailabilityRow> Unavailabilities { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || RosterId == Guid.Empty || MemberId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            await RunTrackedAsync("RosterMemberProfile.Load", async () =>
            {
                var profile = await _rosterService.GetRosterMemberProfileAsync(RosterId, MemberId);
                Apply(profile);
            });
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Apply(RosterMemberProfileDto profile)
    {
        MemberName = profile.MemberName;
        RosterName = profile.RosterName;
        QueueStanding = $"{profile.Rank}º de {profile.QueueSize} na ordem da fila";
        Contact = string.Join(" · ", new[] { profile.Email, profile.Phone }.Where(v => !string.IsNullOrWhiteSpace(v)));
        JoinedQueueAt = $"Na fila desde {profile.JoinedQueueAt:dd/MM/yyyy}";
        IsLinkedToUser = profile.IsLinkedToUser;

        var duties = profile.Duties;
        CompletedCount = duties.Completed.ToString(CultureInfo.InvariantCulture);
        // O número sozinho não diz se a pessoa está sobrecarregada; comparado à média da
        // fila, diz — é essa comparação que o admin usa para decidir reordenar ou não.
        var difference = Math.Round(duties.Completed - duties.QueueAverageCompleted, 1);
        CompletedComparison = difference switch
        {
            0 => $"na média da fila ({duties.QueueAverageCompleted})",
            > 0 => $"+{difference} acima da média ({duties.QueueAverageCompleted})",
            _ => $"{difference} abaixo da média ({duties.QueueAverageCompleted})"
        };
        WorkdayCount = duties.CompletedWorkday.ToString(CultureInfo.InvariantCulture);
        RestDayCount = duties.CompletedRestDay.ToString(CultureInfo.InvariantCulture);
        NextDuty = duties.NextDutyDate is { } next ? next.ToString("dd/MM/yyyy") : "—";
        NextDutyHint = duties.Scheduled > 0
            ? $"{duties.Scheduled} publicado{(duties.Scheduled == 1 ? "" : "s")} à frente"
            : duties.Draft > 0
                ? $"{duties.Draft} em rascunho"
                : "nada agendado";

        var priorities = new[]
        {
            profile.PendingReturnPriorityWorkdayReason is null ? null : $"Dia útil: {profile.PendingReturnPriorityWorkdayReason}",
            profile.PendingReturnPriorityWeekendReason is null ? null : $"Fim de semana: {profile.PendingReturnPriorityWeekendReason}"
        }.Where(p => p is not null);
        ReturnPriorityReason = string.Join("\n", priorities);
        HasReturnPriority = !string.IsNullOrEmpty(ReturnPriorityReason);

        var badges = new[]
        {
            profile.IsWorkdayNextUp ? "Próximo em dia útil" : null,
            profile.IsWeekendNextUp ? "Próximo em fim de semana" : null
        }.Where(b => b is not null);
        NextUpBadges = string.Join(" · ", badges);
        HasNextUpBadges = !string.IsNullOrEmpty(NextUpBadges);

        Fill(UpcomingDuties, profile.UpcomingDuties.Select(DutyRow.From));
        Fill(RecentDuties, profile.RecentDuties.Select(DutyRow.From));
        Fill(Swaps, profile.SwapHistory.Select(SwapRow.From));
        Fill(Unavailabilities, profile.Unavailabilities.Select(UnavailabilityRow.From));
    }

    private static void Fill<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }
}

public record DutyRow(string Date, string Shift, string DayType, string Companions, bool HasCompanions, bool IsDraft)
{
    public static DutyRow From(RosterMemberDutyDto duty) => new(
        duty.Date.ToString("dd/MM/yyyy"),
        $"{duty.ShiftStartTime:HH\\:mm}–{duty.ShiftEndTime:HH\\:mm}",
        duty.IsRestDay ? "Fim de semana" : "Dia útil",
        duty.CompanionNames.Count == 0 ? string.Empty : $"com {string.Join(", ", duty.CompanionNames)}",
        duty.CompanionNames.Count > 0,
        duty.Status == DutyAssignmentStatus.Draft);
}

public record SwapRow(string Date, string Description, string Detail)
{
    public static SwapRow From(RosterMemberSwapDto swap)
    {
        var isOutgoing = swap.Direction == SwapDirection.Out;
        var cause = swap.Cause == SwapCause.PeerSwap ? "Troca entre membros" : "Troca administrativa";

        return new SwapRow(
            swap.Date.ToString("dd/MM/yyyy"),
            isOutgoing
                ? $"Passou o dia para {swap.CounterpartMemberName}"
                : $"Assumiu o dia de {swap.CounterpartMemberName}",
            $"{cause} · {swap.CreatedAt:dd/MM/yyyy HH:mm}");
    }
}

public record UnavailabilityRow(string Period, string Detail, bool IsCurrent, bool IsPending, bool IsRejected)
{
    public static UnavailabilityRow From(RosterMemberUnavailabilityDto period)
    {
        var detail = $"{period.DayCount} dia{(period.DayCount == 1 ? "" : "s")}";
        if (!string.IsNullOrWhiteSpace(period.ReasonName)) detail += $" · {period.ReasonName}";

        return new UnavailabilityRow(
            $"{period.StartDate:dd/MM/yyyy} – {period.EndDate:dd/MM/yyyy}",
            detail,
            period.IsCurrent,
            period.Status == MemberUnavailabilityStatus.Pending,
            period.Status == MemberUnavailabilityStatus.Rejected);
    }
}
