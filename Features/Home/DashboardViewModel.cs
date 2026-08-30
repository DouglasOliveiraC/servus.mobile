using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Features.Login;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.SwapHistory;

namespace Servus.Mobile.Features.Home;

public partial class DashboardViewModel : ObservableObject
{
    private const int UpcomingWindowDays = 14;
    private const int SwapTrendWeeks = 6;
    private const int InitialVisibleDays = 3;
    private const int VisibleDaysIncrement = 3;
    private const int PreviewNamesPerDay = 4;

    private readonly IRosterService _rosterService;
    private readonly IAuthService _authService;
    private readonly ISwapHistoryService _swapHistoryService;

    public DashboardViewModel(IRosterService rosterService, IAuthService authService, ISwapHistoryService swapHistoryService)
    {
        _rosterService = rosterService;
        _authService = authService;
        _swapHistoryService = swapHistoryService;
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? userEmail;

    public string UserInitial => string.IsNullOrEmpty(UserEmail) ? "?" : UserEmail[..1].ToUpperInvariant();

    partial void OnUserEmailChanged(string? value) => OnPropertyChanged(nameof(UserInitial));

    [ObservableProperty]
    private int rosterCount;

    [ObservableProperty]
    private int subrosterCount;

    [ObservableProperty]
    private int upcomingDutyCount;

    [ObservableProperty]
    private int myUpcomingDutyCount;

    [ObservableProperty]
    private int swapCountLastPeriod;

    [ObservableProperty]
    private NextDutyInfo? nextDuty;

    [ObservableProperty]
    private bool isAdminOfAnyRoster;

    [ObservableProperty]
    private int visibleDailyDutyCount = InitialVisibleDays;

    public ObservableCollection<RosterSummary> Rosters { get; } = [];

    public ObservableCollection<UpcomingDuty> UpcomingDuties { get; } = [];

    /// <summary>All days in the upcoming window that have at least one assignment, already
    /// fetched and summarized — VisibleDailyDuties is what the UI actually renders, sliced
    /// from this in memory so "load more" needs no extra network round trip.</summary>
    public ObservableCollection<DailyDutySummary> DailyDuties { get; } = [];

    public ObservableCollection<DailyDutySummary> VisibleDailyDuties { get; } = [];

    public bool HasMoreDailyDuties => DailyDuties.Count > VisibleDailyDutyCount;

    public ObservableCollection<MemberLoadSlice> MemberLoad { get; } = [];

    public ObservableCollection<SwapTrendPoint> SwapTrend { get; } = [];

    public ObservableCollection<CoverageDayPoint> CoverageDays { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            UserEmail = user?.Email;

            var myMemberId = await _rosterService.GetMyMemberIdAsync();
            var rosters = await _rosterService.GetMyRostersAsync();
            IsAdminOfAnyRoster = rosters.Any(r => r.IsAdmin);

            Rosters.Clear();
            UpcomingDuties.Clear();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var windowEnd = today.AddDays(UpcomingWindowDays);

            var totalSubrosters = 0;
            var totalUpcoming = 0;

            var loadByMember = new Dictionary<string, int>();
            var dailyTeam = new Dictionary<DateOnly, int>();
            var dailyRequired = new Dictionary<DateOnly, int>();
            for (var d = today; d <= windowEnd; d = d.AddDays(1))
            {
                dailyTeam[d] = 0;
                dailyRequired[d] = 0;
            }

            var swapEntries = new List<SwapHistoryEntryDto>();

            foreach (var roster in rosters)
            {
                var subrosters = await _rosterService.GetSubrostersAsync(roster.Id);
                totalSubrosters += subrosters.Count;

                var upcomingForRoster = 0;

                foreach (var subroster in subrosters)
                {
                    var schedule = await _rosterService.GetScheduleAsync(subroster.Id, today, windowEnd);

                    if (roster.IsAdmin)
                    {
                        foreach (var day in schedule.Days)
                        {
                            if (dailyRequired.ContainsKey(day.Date) && !day.IsRestDay)
                            {
                                dailyRequired[day.Date] += day.RequiredShiftSize;
                            }
                        }
                    }

                    foreach (var day in schedule.Days.Where(d => d.Assignments.Count > 0).OrderBy(d => d.Date))
                    {
                        upcomingForRoster += day.Assignments.Count;
                        var isMine = myMemberId is not null && day.Assignments.Any(a => a.MemberId == myMemberId);
                        UpcomingDuties.Add(new UpcomingDuty(
                            roster.Name,
                            subroster.Name,
                            day.Date,
                            day.Assignments.Select(a => a.MemberName).ToList(),
                            isMine));

                        if (roster.IsAdmin && dailyTeam.ContainsKey(day.Date))
                        {
                            dailyTeam[day.Date] += day.Assignments.Count;
                        }
                    }

                    var dutyCounts = await _rosterService.GetDutyCountsAsync(subroster.Id);
                    foreach (var count in dutyCounts)
                    {
                        loadByMember[count.MemberName] = loadByMember.GetValueOrDefault(count.MemberName) + count.AssignmentCount;
                    }

                    var history = await _swapHistoryService.GetBySubrosterIdAsync(subroster.Id);
                    swapEntries.AddRange(history);
                }

                totalUpcoming += upcomingForRoster;
                Rosters.Add(new RosterSummary(roster.Id, roster.Name, subrosters.Count, upcomingForRoster));
            }

            RosterCount = rosters.Count;
            SubrosterCount = totalSubrosters;
            UpcomingDutyCount = totalUpcoming;
            MyUpcomingDutyCount = UpcomingDuties.Count(d => d.IsMine);

            var reordered = UpcomingDuties.OrderBy(d => d.Date).ToList();
            UpcomingDuties.Clear();
            foreach (var duty in reordered)
            {
                UpcomingDuties.Add(duty);
            }

            VisibleDailyDutyCount = InitialVisibleDays;
            DailyDuties.Clear();
            foreach (var group in reordered.GroupBy(d => d.Date).OrderBy(g => g.Key))
            {
                DailyDuties.Add(new DailyDutySummary(
                    group.Key,
                    group.Sum(d => d.MemberNames.Count),
                    group.Select(d => d.SubrosterName).Distinct().Count(),
                    group.Any(d => d.IsMine),
                    group.SelectMany(d => d.MemberNames).Take(PreviewNamesPerDay).ToList()));
            }
            RefreshVisibleDailyDuties();

            MemberLoad.Clear();
            foreach (var slice in loadByMember.OrderByDescending(kv => kv.Value).Take(8))
            {
                MemberLoad.Add(new MemberLoadSlice(slice.Key, slice.Value));
            }

            CoverageDays.Clear();
            if (IsAdminOfAnyRoster)
            {
                foreach (var date in dailyRequired.Keys.OrderBy(d => d))
                {
                    CoverageDays.Add(new CoverageDayPoint(
                        date,
                        date.ToString("dd", CultureInfo.InvariantCulture),
                        date.ToString("ddd", CultureInfo.InvariantCulture),
                        dailyTeam[date],
                        dailyRequired[date]));
                }
            }

            var nextMine = UpcomingDuties.FirstOrDefault(d => d.IsMine);
            NextDuty = nextMine is null
                ? null
                : new NextDutyInfo(nextMine.RosterName, nextMine.SubrosterName, nextMine.Date, nextMine.Date.DayNumber - today.DayNumber);

            BuildSwapTrend(swapEntries, today);
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

    [RelayCommand]
    private void ShowMoreDailyDuties()
    {
        VisibleDailyDutyCount += VisibleDaysIncrement;
        RefreshVisibleDailyDuties();
    }

    /// <summary>Re-slices DailyDuties (already fully fetched) into VisibleDailyDuties — the
    /// "load more" button reveals more of what's already in memory, no network round trip.</summary>
    private void RefreshVisibleDailyDuties()
    {
        VisibleDailyDuties.Clear();
        foreach (var day in DailyDuties.Take(VisibleDailyDutyCount))
        {
            VisibleDailyDuties.Add(day);
        }

        OnPropertyChanged(nameof(HasMoreDailyDuties));
    }

    [RelayCommand]
    private async Task OpenDayDetailAsync(DateOnly date)
    {
        await Shell.Current.GoToAsync(nameof(DayDetailPage), new Dictionary<string, object>
        {
            ["date"] = date.ToString("yyyy-MM-dd")
        });
    }

    private void BuildSwapTrend(List<SwapHistoryEntryDto> swapEntries, DateOnly today)
    {
        SwapTrend.Clear();

        var weekStart = today.AddDays(-((int)today.DayOfWeek)).AddDays(-7 * (SwapTrendWeeks - 1));
        var total = 0;

        for (var week = 0; week < SwapTrendWeeks; week++)
        {
            var start = weekStart.AddDays(7 * week);
            var end = start.AddDays(6);
            var count = swapEntries.Count(e => e.Date >= start && e.Date <= end);
            total += count;
            SwapTrend.Add(new SwapTrendPoint($"{start:dd/MM}", count));
        }

        SwapCountLastPeriod = total;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    [RelayCommand]
    private static async Task GoToQuickActionAsync(string route) => await Shell.Current.GoToAsync(route);
}
