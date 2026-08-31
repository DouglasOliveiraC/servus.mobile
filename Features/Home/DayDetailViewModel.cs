using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Organizations;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Home;

/// <summary>
/// Date arrives as a "yyyy-MM-dd" query string, not a DateOnly — Shell query params only
/// support simple primitives/strings, matching the rest of the app's navigation (see
/// RosterScheduleViewModel's Guid QueryProperty). The page re-fetches assignments for
/// every roster the user belongs to and filters down to this one date, the same scan
/// DashboardViewModel already does for the whole window — there is no per-day endpoint.
/// </summary>
[QueryProperty(nameof(DateText), "date")]
public partial class DayDetailViewModel : ObservableObject
{
    private readonly IOrganizationService _organizationService;
    private readonly IRosterService _rosterService;

    public DayDetailViewModel(IOrganizationService organizationService, IRosterService rosterService)
    {
        _organizationService = organizationService;
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial string DateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateOnly Date { get; set; }

    [ObservableProperty]
    public partial string DateLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    public ObservableCollection<DayRosterGroup> Groups { get; } = [];

    partial void OnDateTextChanged(string value)
    {
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            Date = parsed;
            var label = parsed.ToString("dddd, dd/MM/yyyy", new CultureInfo("pt-BR"));
            DateLabel = char.ToUpper(label[0], new CultureInfo("pt-BR")) + label[1..];
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || Date == default) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var organizations = await _organizationService.GetMyOrganizationsAsync();

            Groups.Clear();
            foreach (var organization in organizations)
            {
                var rosters = await _rosterService.GetRostersAsync(organization.Id);
                foreach (var roster in rosters)
                {
                    var schedule = await _rosterService.GetScheduleAsync(roster.Id, Date, Date);
                    var day = schedule.Days.FirstOrDefault(d => d.Date == Date);
                    if (day is null || day.Assignments.Count == 0) continue;

                    Groups.Add(new DayRosterGroup(
                        roster.Id,
                        organization.Name,
                        roster.Name,
                        day.Assignments.Select(a => a.MemberName).ToList()));
                }
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
    private async Task OpenEditAsync(DayRosterGroup group)
    {
        await Shell.Current.GoToAsync(nameof(DayScheduleEditPage), new Dictionary<string, object>
        {
            ["rosterId"] = group.RosterId,
            ["date"] = DateText
        });
    }
}
