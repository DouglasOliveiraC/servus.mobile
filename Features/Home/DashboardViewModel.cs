using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Features.Menu;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Notifications;
using Servus.Mobile.Services.Organizations;
using Servus.Mobile.Services.Push;
using Servus.Mobile.Services.Realtime;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.SwapHistory;
using Servus.Mobile.Services.SwapRequests;
using Servus.Mobile.Services.Telemetry;
using Servus.Mobile.Services.Users;

namespace Servus.Mobile.Features.Home;

// Mirrors servus.web's DashboardPage: one screen, two layouts picked by whether the user
// administers any organization (AdminDashboard vs MemberDashboard there).
public partial class DashboardViewModel : BaseViewModel
{
    private const int UpcomingWindowDays = 14;
    private const int SwapTrendWeeks = 6;
    private const int InitialVisibleDays = 3;
    private const int VisibleDaysIncrement = 3;
    private const int PreviewNamesPerDay = 4;
    private const int MyShiftsShown = 5;
    private const int OnDutyTodayShown = 5;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IOrganizationService _organizationService;
    private readonly IRosterService _rosterService;
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ISwapHistoryService _swapHistoryService;
    private readonly ISwapRequestService _swapRequestService;
    private readonly IRealtimeClient _realtimeClient;
    private readonly ILocalNotifier _notifier;
    private readonly IPushRegistrationService _pushRegistration;
    private readonly AppMenuViewModel _appMenu;

    public DashboardViewModel(
        IOrganizationService organizationService,
        IRosterService rosterService,
        IAuthService authService,
        IUserService userService,
        ISwapHistoryService swapHistoryService,
        ISwapRequestService swapRequestService,
        IRealtimeClient realtimeClient,
        ILocalNotifier notifier,
        IPushRegistrationService pushRegistration,
        AppMenuViewModel appMenu,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _organizationService = organizationService;
        _rosterService = rosterService;
        _authService = authService;
        _userService = userService;
        _swapHistoryService = swapHistoryService;
        _swapRequestService = swapRequestService;
        _realtimeClient = realtimeClient;
        _notifier = notifier;
        _pushRegistration = pushRegistration;
        _appMenu = appMenu;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    // Separate from IsBusy on purpose: the RefreshView sets this itself on pull-to-refresh and never
    // clears it, so it must be reset after every load. Binding it to IsBusy instead would show the
    // pull spinner on every OnAppearing load, duplicating the inline indicator next to the greeting.
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLoadingPlaceholder))]
    public partial bool HasError { get; set; }

    // Neither layout renders before the first load finishes: until then IsAdminOfAnyOrganization
    // reads false and an admin would briefly see the member layout (same guard as the web).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdminView), nameof(IsMemberView), nameof(ShowLoadingPlaceholder), nameof(ShowEmptyState))]
    public partial bool HasLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdminView), nameof(IsMemberView))]
    public partial bool IsAdminOfAnyOrganization { get; set; }

    public bool IsAdminView => HasLoaded && IsAdminOfAnyOrganization;

    public bool IsMemberView => HasLoaded && !IsAdminOfAnyOrganization;

    public bool ShowLoadingPlaceholder => !HasLoaded && !HasError;

    public bool ShowEmptyState => HasLoaded && OrganizationCount == 0;

    [ObservableProperty]
    public partial string? UserDisplayName { get; set; }

    [ObservableProperty]
    public partial string? UserEmail { get; set; }

    // First token of DisplayName (e-mail as fallback), for both greeting and avatar,
    // because the JWT "sub" carries no name.
    public string UserFirstName => string.IsNullOrWhiteSpace(UserDisplayName)
        ? UserEmail?.Split('@')[0] ?? string.Empty
        : UserDisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    public string UserInitial => string.IsNullOrWhiteSpace(UserFirstName) ? "?" : UserFirstName[..1].ToUpperInvariant();

    // No enforced capitalization so names like "lucas" aren't distorted (handling exceptions
    // like "de/silva" would make a correct TitleCase too verbose for the gain).
    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var period = hour switch
            {
                < 5 => "Boa madrugada",
                < 12 => "Bom dia",
                < 18 => "Boa tarde",
                _ => "Boa noite"
            };
            return string.IsNullOrWhiteSpace(UserFirstName) ? "Olá" : $"{period}, {UserFirstName}";
        }
    }

    public string TodayLabel
    {
        get
        {
            var text = DateTime.Today.ToString("dddd, d 'de' MMMM", PtBr);
            return char.ToUpper(text[0], PtBr) + text[1..];
        }
    }

    partial void OnUserDisplayNameChanged(string? value) => NotifyIdentityChanged();

    partial void OnUserEmailChanged(string? value) => NotifyIdentityChanged();

    private void NotifyIdentityChanged()
    {
        OnPropertyChanged(nameof(UserFirstName));
        OnPropertyChanged(nameof(UserInitial));
        OnPropertyChanged(nameof(Greeting));
    }

    // Greeting/TodayLabel/NextShiftWhen read the clock on get, but Shell caches this page and
    // keeps it alive across days in the background — without re-notifying, the binding keeps
    // whatever was computed when the page was first built (e.g. "Boa madrugada" at 21h).
    public void RefreshClockLabels()
    {
        OnPropertyChanged(nameof(Greeting));
        OnPropertyChanged(nameof(TodayLabel));
        OnPropertyChanged(nameof(NextShiftWhen));
    }

    // --- Shared ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(OrganizationHint))]
    public partial int OrganizationCount { get; set; }

    public string OrganizationHint => OrganizationCount == 1 ? "em 1 organização" : $"em {OrganizationCount} organizações";

    [ObservableProperty]
    public partial int RosterCount { get; set; }

    [ObservableProperty]
    public partial int PendingSwapCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNextShift), nameof(HasNoNextShift), nameof(NextShiftWhen), nameof(NextShiftDayLabel), nameof(NextShiftMonthLabel), nameof(NextShiftWeekdayLabel))]
    public partial MyShift? NextShift { get; set; }

    public bool HasNextShift => NextShift is not null;

    public bool HasNoNextShift => NextShift is null;

    public string NextShiftWhen => NextShift is null
        ? string.Empty
        : (NextShift.Date.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber) switch
        {
            0 => "Hoje",
            1 => "Amanhã",
            var days => $"Em {days} dias"
        };

    public string NextShiftDayLabel => NextShift?.Date.ToString("dd", PtBr) ?? string.Empty;

    public string NextShiftMonthLabel => NextShift?.Date.ToString("MMM", PtBr).TrimEnd('.').ToUpperInvariant() ?? string.Empty;

    public string NextShiftWeekdayLabel => NextShift?.Date.ToString("dddd", PtBr) ?? string.Empty;

    public ObservableCollection<MyShift> MyUpcomingShifts { get; } = [];

    // --- Member ("servidor") ---

    [ObservableProperty]
    public partial int MonthDutyCount { get; set; }

    [ObservableProperty]
    public partial string MonthSplitLabel { get; set; } = "0 / 0";

    [ObservableProperty]
    public partial string NextRestDayLabel { get; set; } = "—";

    public ObservableCollection<MyRosterSummary> MyRosters { get; } = [];

    // --- Admin ---

    [ObservableProperty]
    public partial int MemberCount { get; set; }

    [ObservableProperty]
    public partial string CoverageLabel { get; set; } = "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnderstaffedText))]
    public partial int UnderstaffedDayCount { get; set; }

    public string UnderstaffedText => UnderstaffedDayCount == 1
        ? "1 dia com falta de gente nos próximos 14 dias"
        : $"{UnderstaffedDayCount} dias com falta de gente nos próximos 14 dias";

    public string OnDutyTodaySubtitle => OnDutyToday.Count == 1
        ? "1 plantão em operação"
        : $"{OnDutyToday.Count} plantões em operação";

    [ObservableProperty]
    public partial int SwapCountLastPeriod { get; set; }

    [ObservableProperty]
    public partial int VisibleDailyDutyCount { get; set; } = InitialVisibleDays;

    public ObservableCollection<OnDutyEntry> OnDutyToday { get; } = [];

    // Capped because a busy day listed 14+ people and pushed coverage off the first screen;
    // the full list is the day detail page, one tap away.
    public ObservableCollection<OnDutyEntry> OnDutyTodayPreview { get; } = [];

    public bool HasMoreOnDutyToday => OnDutyToday.Count > OnDutyTodayShown;

    public string OnDutyTodayMoreText => $"Ver todos os {OnDutyToday.Count}";

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
        RefreshClockLabels();
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            UserEmail = user?.Email;
            var isDemo = user?.IsDemo == true;
            if (user is not null)
            {
                // A failing profile endpoint must not take the dashboard down — the greeting
                // falls back to the e-mail instead.
                try
                {
                    UserDisplayName = (await _userService.GetByIdAsync(user.Id)).DisplayName;
                }
                catch (HttpRequestException)
                {
                    UserDisplayName = null;
                }
            }
            await _realtimeClient.ConnectAsync();
            // Not awaited: the system dialog would otherwise hold the dashboard load until
            // the user answers it.
            _ = _notifier.EnsurePermissionAsync();
            // Demo accounts are refused by the API: a throwaway sandbox has no phone to reach.
            if (user is not null && !isDemo)
            {
                _ = _pushRegistration.RegisterAsync();
            }

            var myMemberId = await _rosterService.GetMyMemberIdAsync();
            var organizations = await _organizationService.GetMyOrganizationsAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var windowEnd = today.AddDays(UpcomingWindowDays);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            // One fetch per roster covers both the member's month KPIs and the 14-day window.
            var fetchEnd = windowEnd > monthEnd ? windowEnd : monthEnd;

            var myShifts = new List<MyShift>();
            var dayEntries = new List<(DateOnly Date, string RosterName, IReadOnlyList<string> Names, bool IsMine)>();
            var onDutyToday = new List<OnDutyEntry>();
            var loadByMember = new Dictionary<string, int>();
            var adminMemberIds = new HashSet<Guid>();
            var dailyTeam = new Dictionary<DateOnly, int>();
            var dailyRequired = new Dictionary<DateOnly, int>();
            for (var d = today; d <= windowEnd; d = d.AddDays(1))
            {
                dailyTeam[d] = 0;
                dailyRequired[d] = 0;
            }

            var swapEntries = new List<SwapHistoryEntryDto>();
            var rosters = new List<MyRosterSummary>();

            foreach (var organization in organizations)
            {
                var organizationRosters = await _rosterService.GetRostersAsync(organization.Id);

                foreach (var roster in organizationRosters)
                {
                    rosters.Add(new MyRosterSummary(roster.Id, roster.Name, organization.Name));
                    var schedule = await _rosterService.GetScheduleAsync(roster.Id, monthStart, fetchEnd);

                    foreach (var day in schedule.Days)
                    {
                        // The roster schedule also returns drafts, even to plain members. A shift
                        // only counts as "mine" once published, matching the web's /api/me/schedule;
                        // the admin panels below still see drafts on purpose.
                        var published = day.Assignments.Where(a => a.Status == DutyAssignmentStatus.Published).ToList();
                        var mine = myMemberId is null
                            ? null
                            : published.FirstOrDefault(a => a.MemberId == myMemberId);
                        if (mine is not null)
                        {
                            myShifts.Add(new MyShift(
                                day.Date,
                                roster.Name,
                                organization.Name,
                                mine.ShiftStartTime,
                                mine.ShiftEndTime,
                                day.IsRestDay,
                                published.Where(a => a.MemberId != myMemberId).Select(a => a.MemberName).ToList()));
                        }

                        if (!organization.IsAdmin || day.Date < today || day.Date > windowEnd) continue;

                        // Rest days count with RestDayShiftSize (> 0 when there's weekend duty) —
                        // excluding them painted Sat/Sun grey even with people assigned.
                        dailyRequired[day.Date] += day.RequiredShiftSize;
                        dailyTeam[day.Date] += day.Assignments.Count;

                        if (day.Assignments.Count > 0)
                        {
                            dayEntries.Add((day.Date, roster.Name, day.Assignments.Select(a => a.MemberName).ToList(), mine is not null));
                        }

                        if (day.Date == today)
                        {
                            onDutyToday.AddRange(day.Assignments.Select(a =>
                                new OnDutyEntry(a.MemberName, roster.Name, a.ShiftStartTime, a.ShiftEndTime)));
                        }
                    }

                    // Load and swap charts are management views: a plain member has no use for
                    // them, and fetching them for every roster was most of the member's load time.
                    if (!organization.IsAdmin) continue;

                    var dutyCounts = await _rosterService.GetDutyCountsAsync(roster.Id);
                    foreach (var count in dutyCounts)
                    {
                        adminMemberIds.Add(count.MemberId);
                        loadByMember[count.MemberName] = loadByMember.GetValueOrDefault(count.MemberName) + count.AssignmentCount;
                    }

                    swapEntries.AddRange(await _swapHistoryService.GetByRosterIdAsync(roster.Id));
                }
            }

            PendingSwapCount = myMemberId is null
                ? 0
                : (await _swapRequestService.GetPendingAsync(myMemberId.Value)).Count;

            OrganizationCount = organizations.Count;
            RosterCount = rosters.Count;
            IsAdminOfAnyOrganization = organizations.Any(o => o.IsAdmin);

            ReplaceAll(MyRosters, rosters);
            ApplyMyShifts(myShifts, today, monthStart, monthEnd);

            if (IsAdminOfAnyOrganization)
            {
                ApplyAdminData(dayEntries, onDutyToday, loadByMember, dailyTeam, dailyRequired);
                MemberCount = adminMemberIds.Count;
                BuildSwapTrend(swapEntries, today);
            }

            _appMenu.SetUser(UserEmail, UserInitial, IsAdminOfAnyOrganization, isDemo);
            HasLoaded = true;
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }

        // A notification tapped with the app closed only gets here once login and this load are
        // done; navigating earlier would land on a screen without a session behind it.
        if (NotificationLaunch.TakePendingRoute() is { } pendingRoute)
        {
            await NavigationService.GoToAsync(pendingRoute);
        }
    }

    private void ApplyMyShifts(List<MyShift> myShifts, DateOnly today, DateOnly monthStart, DateOnly monthEnd)
    {
        var inMonth = myShifts.Where(s => s.Date >= monthStart && s.Date <= monthEnd).ToList();
        MonthDutyCount = inMonth.Count;
        MonthSplitLabel = $"{inMonth.Count(s => !s.IsRestDay)} / {inMonth.Count(s => s.IsRestDay)}";

        var upcoming = myShifts.Where(s => s.Date >= today).OrderBy(s => s.Date).ThenBy(s => s.ShiftStart).ToList();
        NextShift = upcoming.FirstOrDefault();

        var nextRest = upcoming.FirstOrDefault(s => s.IsRestDay);
        NextRestDayLabel = nextRest is null ? "—" : nextRest.Date.ToString("dd/MM", PtBr);

        ReplaceAll(MyUpcomingShifts, upcoming.Take(MyShiftsShown));
    }

    private void ApplyAdminData(
        List<(DateOnly Date, string RosterName, IReadOnlyList<string> Names, bool IsMine)> dayEntries,
        List<OnDutyEntry> onDutyToday,
        Dictionary<string, int> loadByMember,
        Dictionary<DateOnly, int> dailyTeam,
        Dictionary<DateOnly, int> dailyRequired)
    {
        ReplaceAll(OnDutyToday, onDutyToday.OrderBy(e => e.ShiftStart).ThenBy(e => e.MemberName));
        ReplaceAll(OnDutyTodayPreview, OnDutyToday.Take(OnDutyTodayShown));
        OnPropertyChanged(nameof(OnDutyTodaySubtitle));
        OnPropertyChanged(nameof(HasMoreOnDutyToday));
        OnPropertyChanged(nameof(OnDutyTodayMoreText));

        VisibleDailyDutyCount = InitialVisibleDays;
        ReplaceAll(DailyDuties, dayEntries
            .GroupBy(e => e.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyDutySummary(
                g.Key,
                g.Sum(e => e.Names.Count),
                g.Select(e => e.RosterName).Distinct().Count(),
                g.Any(e => e.IsMine),
                g.SelectMany(e => e.Names).Take(PreviewNamesPerDay).ToList())));
        RefreshVisibleDailyDuties();

        ReplaceAll(MemberLoad, loadByMember
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select((kv, i) => new MemberLoadSlice(kv.Key, kv.Value, PaletteColor(i))));

        ReplaceAll(CoverageDays, dailyRequired.Keys.OrderBy(d => d).Select(date => new CoverageDayPoint(
            date,
            date.ToString("dd", PtBr),
            date.ToString("ddd", PtBr).TrimEnd('.'),
            dailyTeam[date],
            dailyRequired[date])));

        UnderstaffedDayCount = CoverageDays.Count(c => c.HasGap);

        // Same formula as the web's "Cobertura (mês)" card: over-staffing on one day must not
        // hide a gap on another, hence the per-day Math.Min.
        var required = CoverageDays.Sum(c => c.Required);
        CoverageLabel = required == 0
            ? "—"
            : $"{Math.Round(CoverageDays.Sum(c => Math.Min(c.Assigned, c.Required)) * 100.0 / required)}%";
    }

    private static Color PaletteColor(int index) =>
        ((SolidColorBrush)DashboardPalette.Brushes[index % DashboardPalette.Brushes.Count]).Color;

    private static void ReplaceAll<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
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
        ReplaceAll(VisibleDailyDuties, DailyDuties.Take(VisibleDailyDutyCount));
        OnPropertyChanged(nameof(HasMoreDailyDuties));
    }

    [RelayCommand]
    private Task OpenDayDetailAsync(DateOnly date) =>
        NavigationService.GoToAsync(Routes.DayDetail, new Dictionary<string, object>
        {
            ["date"] = date.ToString("yyyy-MM-dd")
        });

    [RelayCommand]
    private Task OpenTodayDetailAsync() => OpenDayDetailAsync(DateOnly.FromDateTime(DateTime.Today));

    [RelayCommand]
    private Task OpenRosterScheduleAsync(Guid rosterId) =>
        NavigationService.GoToAsync(Routes.RosterSchedule, new Dictionary<string, object>
        {
            ["rosterId"] = rosterId
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
    private Task GoToQuickActionAsync(string route) => NavigationService.GoToAsync(route);
}
