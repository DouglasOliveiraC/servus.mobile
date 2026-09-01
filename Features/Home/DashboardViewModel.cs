using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Organizations;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.SwapHistory;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Features.Home;

public partial class DashboardViewModel : BaseViewModel
{
    private const int UpcomingWindowDays = 14;
    private const int SwapTrendWeeks = 6;
    private const int InitialVisibleDays = 3;
    private const int VisibleDaysIncrement = 3;
    private const int PreviewNamesPerDay = 4;

    private readonly IOrganizationService _organizationService;
    private readonly IRosterService _rosterService;
    private readonly IAuthService _authService;
    private readonly ISwapHistoryService _swapHistoryService;

    public DashboardViewModel(
        IOrganizationService organizationService,
        IRosterService rosterService,
        IAuthService authService,
        ISwapHistoryService swapHistoryService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _organizationService = organizationService;
        _rosterService = rosterService;
        _authService = authService;
        _swapHistoryService = swapHistoryService;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string? UserEmail { get; set; }

    public string UserInitial => string.IsNullOrEmpty(UserEmail) ? "?" : UserEmail[..1].ToUpperInvariant();

    partial void OnUserEmailChanged(string? value) => OnPropertyChanged(nameof(UserInitial));

    [ObservableProperty]
    public partial int OrganizationCount { get; set; }

    [ObservableProperty]
    public partial int RosterCount { get; set; }

    [ObservableProperty]
    public partial int UpcomingDutyCount { get; set; }

    [ObservableProperty]
    public partial int MyUpcomingDutyCount { get; set; }

    [ObservableProperty]
    public partial int SwapCountLastPeriod { get; set; }

    [ObservableProperty]
    public partial NextDutyInfo? NextDuty { get; set; }

    [ObservableProperty]
    public partial bool IsAdminOfAnyOrganization { get; set; }

    [ObservableProperty]
    public partial int VisibleDailyDutyCount { get; set; } = InitialVisibleDays;

    public ObservableCollection<OrganizationSummary> Organizations { get; } = [];

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
            var organizations = await _organizationService.GetMyOrganizationsAsync();
            IsAdminOfAnyOrganization = organizations.Any(o => o.IsAdmin);

            Organizations.Clear();
            UpcomingDuties.Clear();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var windowEnd = today.AddDays(UpcomingWindowDays);

            var totalRosters = 0;
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

            foreach (var organization in organizations)
            {
                var rosters = await _rosterService.GetRostersAsync(organization.Id);
                totalRosters += rosters.Count;

                var upcomingForOrganization = 0;

                foreach (var roster in rosters)
                {
                    var schedule = await _rosterService.GetScheduleAsync(roster.Id, today, windowEnd);

                    if (organization.IsAdmin)
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
                        upcomingForOrganization += day.Assignments.Count;
                        var isMine = myMemberId is not null && day.Assignments.Any(a => a.MemberId == myMemberId);
                        UpcomingDuties.Add(new UpcomingDuty(
                            organization.Name,
                            roster.Name,
                            day.Date,
                            day.Assignments.Select(a => a.MemberName).ToList(),
                            isMine));

                        if (organization.IsAdmin && dailyTeam.ContainsKey(day.Date))
                        {
                            dailyTeam[day.Date] += day.Assignments.Count;
                        }
                    }

                    var dutyCounts = await _rosterService.GetDutyCountsAsync(roster.Id);
                    foreach (var count in dutyCounts)
                    {
                        loadByMember[count.MemberName] = loadByMember.GetValueOrDefault(count.MemberName) + count.AssignmentCount;
                    }

                    var history = await _swapHistoryService.GetByRosterIdAsync(roster.Id);
                    swapEntries.AddRange(history);
                }

                totalUpcoming += upcomingForOrganization;
                Organizations.Add(new OrganizationSummary(organization.Id, organization.Name, rosters.Count, upcomingForOrganization));
            }

            OrganizationCount = organizations.Count;
            RosterCount = totalRosters;
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
                    group.Select(d => d.RosterName).Distinct().Count(),
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
            if (IsAdminOfAnyOrganization)
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
                : new NextDutyInfo(nextMine.OrganizationName, nextMine.RosterName, nextMine.Date, nextMine.Date.DayNumber - today.DayNumber);

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
    private Task OpenDayDetailAsync(DateOnly date) =>
        NavigationService.GoToAsync(Routes.DayDetail, new Dictionary<string, object>
        {
            ["date"] = date.ToString("yyyy-MM-dd")
        });

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
        await NavigationService.GoToRootAsync(Routes.Login);
    }

    [RelayCommand]
    private Task GoToQuickActionAsync(string route) => NavigationService.GoToAsync(route);
}
