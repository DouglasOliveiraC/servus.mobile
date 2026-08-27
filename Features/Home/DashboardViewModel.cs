using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Features.Login;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Home;

public partial class DashboardViewModel : ObservableObject
{
    private const int UpcomingWindowDays = 14;

    private readonly IRosterService _rosterService;
    private readonly IAuthService _authService;

    public DashboardViewModel(IRosterService rosterService, IAuthService authService)
    {
        _rosterService = rosterService;
        _authService = authService;
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? userEmail;

    [ObservableProperty]
    private int rosterCount;

    [ObservableProperty]
    private int subrosterCount;

    [ObservableProperty]
    private int upcomingDutyCount;

    [ObservableProperty]
    private int myUpcomingDutyCount;

    public ObservableCollection<RosterSummary> Rosters { get; } = [];

    public ObservableCollection<UpcomingDuty> UpcomingDuties { get; } = [];

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

            Rosters.Clear();
            UpcomingDuties.Clear();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var windowEnd = today.AddDays(UpcomingWindowDays);

            var totalSubrosters = 0;
            var totalUpcoming = 0;

            foreach (var roster in rosters)
            {
                var subrosters = await _rosterService.GetSubrostersAsync(roster.Id);
                totalSubrosters += subrosters.Count;

                var upcomingForRoster = 0;

                foreach (var subroster in subrosters)
                {
                    var schedule = await _rosterService.GetScheduleAsync(subroster.Id, today, windowEnd);

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
                    }
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
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
