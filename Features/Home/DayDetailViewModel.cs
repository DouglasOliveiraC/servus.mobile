using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Home;

/// <summary>
/// Date arrives as a "yyyy-MM-dd" query string, not a DateOnly — Shell query params only
/// support simple primitives/strings, matching the rest of the app's navigation (see
/// SubrosterScheduleViewModel's Guid QueryProperty). The page re-fetches assignments for
/// every subroster the user belongs to and filters down to this one date, the same scan
/// DashboardViewModel already does for the whole window — there is no per-day endpoint.
/// </summary>
[QueryProperty(nameof(DateText), "date")]
public partial class DayDetailViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;

    public DayDetailViewModel(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    private string dateText = string.Empty;

    [ObservableProperty]
    private DateOnly date;

    [ObservableProperty]
    private string dateLabel = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    public ObservableCollection<DaySubrosterGroup> Groups { get; } = [];

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
            var rosters = await _rosterService.GetMyRostersAsync();

            Groups.Clear();
            foreach (var roster in rosters)
            {
                var subrosters = await _rosterService.GetSubrostersAsync(roster.Id);
                foreach (var subroster in subrosters)
                {
                    var schedule = await _rosterService.GetScheduleAsync(subroster.Id, Date, Date);
                    var day = schedule.Days.FirstOrDefault(d => d.Date == Date);
                    if (day is null || day.Assignments.Count == 0) continue;

                    Groups.Add(new DaySubrosterGroup(
                        subroster.Id,
                        roster.Name,
                        subroster.Name,
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
    private async Task OpenEditAsync(DaySubrosterGroup group)
    {
        await Shell.Current.GoToAsync(nameof(DayScheduleEditPage), new Dictionary<string, object>
        {
            ["subrosterId"] = group.SubrosterId,
            ["date"] = DateText
        });
    }
}
