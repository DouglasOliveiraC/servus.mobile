using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(RosterId), "rosterId")]
[QueryProperty(nameof(RosterName), "rosterName")]
public partial class SubrosterListViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;

    public SubrosterListViewModel(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    private Guid rosterId;

    [ObservableProperty]
    private string rosterName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isFormVisible;

    [ObservableProperty]
    private string newName = string.Empty;

    [ObservableProperty]
    private string newMinIntervalDays = "1";

    [ObservableProperty]
    private TimeSpan newServiceDayStartTime = new(8, 0, 0);

    [ObservableProperty]
    private string newMinDaysAfterReturnBeforeEligible = "0";

    [ObservableProperty]
    private bool newRequiresAdminApproval;

    [ObservableProperty]
    private bool newCreatorIsMember = true;

    public ObservableCollection<SubrosterDto> Subrosters { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || RosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var subrosters = await _rosterService.GetSubrostersAsync(RosterId);
            Subrosters.Clear();
            foreach (var subroster in subrosters)
            {
                Subrosters.Add(subroster);
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
    private void ToggleForm() => IsFormVisible = !IsFormVisible;

    [RelayCommand]
    private async Task CreateSubrosterAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;

        var minInterval = int.TryParse(NewMinIntervalDays, out var mi) ? mi : 1;
        var minDaysAfterReturn = int.TryParse(NewMinDaysAfterReturnBeforeEligible, out var md) ? md : 0;
        var serviceDayStartTime = TimeOnly.FromTimeSpan(NewServiceDayStartTime);

        try
        {
            // serviceShifts: null auto-generates the default set (six 4-hour shifts) seeded
            // from serviceDayStartTime — fine-tuning shift count/duration/capacity happens in
            // SubrosterSettingsPage after creation, not here.
            await _rosterService.CreateSubrosterAsync(RosterId, NewName.Trim(), minInterval, NewRequiresAdminApproval, NewCreatorIsMember, minDaysAfterReturn, fillShiftsEarliestFirst: true, serviceDayStartTime, serviceShifts: null);
            NewName = string.Empty;
            NewMinIntervalDays = "1";
            NewServiceDayStartTime = new TimeSpan(8, 0, 0);
            NewMinDaysAfterReturnBeforeEligible = "0";
            NewRequiresAdminApproval = false;
            NewCreatorIsMember = true;
            IsFormVisible = false;
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível criar a escala.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteSubrosterAsync(SubrosterDto subroster)
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir escala",
            $"Esta ação remove todos os vínculos e acessos de membros com \"{subroster.Name}\" e não pode ser desfeita. Os membros continuam cadastrados no sistema.\n\nDigite \"{subroster.Name}\" para confirmar:");

        if (typed?.Trim() != subroster.Name) return;

        try
        {
            await _rosterService.DeleteSubrosterAsync(subroster.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a escala.", "OK");
        }
    }

    [RelayCommand]
    private static async Task OpenSubrosterAsync(SubrosterDto subroster)
    {
        await Shell.Current.GoToAsync(nameof(SubrosterDetailPage), new Dictionary<string, object>
        {
            ["subrosterId"] = subroster.Id,
            ["subrosterName"] = subroster.Name
        });
    }
}
