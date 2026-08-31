using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(OrganizationId), "organizationId")]
[QueryProperty(nameof(OrganizationName), "organizationName")]
[QueryProperty(nameof(IsOrganizationAdmin), "isOrganizationAdmin")]
public partial class RosterListViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;

    public RosterListViewModel(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial Guid OrganizationId { get; set; }

    [ObservableProperty]
    public partial string OrganizationName { get; set; } = string.Empty;

    /// <summary>Only gates the "Nova escala" button's visibility — the API re-checks
    /// CanManageOrganizationAsync on every write, so this is UX only, never the real gate.</summary>
    [ObservableProperty]
    public partial bool IsOrganizationAdmin { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsFormVisible { get; set; }

    [ObservableProperty]
    public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMinIntervalDays { get; set; } = "1";

    [ObservableProperty]
    public partial TimeSpan NewServiceDayStartTime { get; set; } = new(8, 0, 0);

    [ObservableProperty]
    public partial string NewMinDaysAfterReturnBeforeEligible { get; set; } = "0";

    [ObservableProperty]
    public partial bool NewRequiresAdminApproval { get; set; }

    [ObservableProperty]
    public partial bool NewCreatorIsMember { get; set; } = true;

    public ObservableCollection<RosterDto> Rosters { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || OrganizationId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var rosters = await _rosterService.GetRostersAsync(OrganizationId);
            Rosters.Clear();
            foreach (var roster in rosters)
            {
                Rosters.Add(roster);
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
    private async Task CreateRosterAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;

        var minInterval = int.TryParse(NewMinIntervalDays, out var mi) ? mi : 1;
        var minDaysAfterReturn = int.TryParse(NewMinDaysAfterReturnBeforeEligible, out var md) ? md : 0;
        var serviceDayStartTime = TimeOnly.FromTimeSpan(NewServiceDayStartTime);

        try
        {
            // serviceShifts: null auto-generates the default set (six 4-hour shifts) seeded
            // from serviceDayStartTime — fine-tuning shift count/duration/capacity happens in
            // RosterSettingsPage after creation, not here.
            await _rosterService.CreateRosterAsync(OrganizationId, NewName.Trim(), minInterval, NewRequiresAdminApproval, NewCreatorIsMember, minDaysAfterReturn, fillShiftsEarliestFirst: true, serviceDayStartTime, QueueDirection.TopToBottom, QueueDirection.TopToBottom, serviceShifts: null);
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
    private async Task DeleteRosterAsync(RosterDto roster)
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir escala",
            $"Esta ação remove todos os vínculos e acessos de membros com \"{roster.Name}\" e não pode ser desfeita. Os membros continuam cadastrados no sistema.\n\nDigite \"{roster.Name}\" para confirmar:");

        if (typed?.Trim() != roster.Name) return;

        try
        {
            await _rosterService.DeleteRosterAsync(roster.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a escala.", "OK");
        }
    }

    [RelayCommand]
    private static async Task OpenRosterAsync(RosterDto roster)
    {
        await Shell.Current.GoToAsync(nameof(RosterDetailPage), new Dictionary<string, object>
        {
            ["rosterId"] = roster.Id,
            ["rosterName"] = roster.Name
        });
    }
}
