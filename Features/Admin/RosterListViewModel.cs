using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Admin;

public partial class RosterListViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;

    public RosterListViewModel(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    public ObservableCollection<RosterDto> Rosters { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var rosters = await _rosterService.GetMyRostersAsync();
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
    private async Task CreateRosterAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("Nova organização", "Nome da organização (ex.: hospital, batalhão, equipe):");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await _rosterService.CreateRosterAsync(name.Trim());
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível criar a organização.", "OK");
        }
    }

    [RelayCommand]
    private async Task RenameRosterAsync(RosterDto roster)
    {
        var name = await Shell.Current.DisplayPromptAsync("Renomear organização", "Novo nome:", initialValue: roster.Name);
        if (string.IsNullOrWhiteSpace(name) || name.Trim() == roster.Name) return;

        try
        {
            await _rosterService.UpdateRosterNameAsync(roster.Id, name.Trim());
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível renomear a organização.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteRosterAsync(RosterDto roster)
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir organização",
            $"Esta ação exclui também todas as escalas e remove os vínculos e acessos de membros com \"{roster.Name}\" e não pode ser desfeita. Os membros continuam cadastrados no sistema.\n\nDigite \"{roster.Name}\" para confirmar:");

        if (typed?.Trim() != roster.Name) return;

        try
        {
            await _rosterService.DeleteRosterAsync(roster.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a organização.", "OK");
        }
    }

    [RelayCommand]
    private static async Task OpenRosterAsync(RosterDto roster)
    {
        await Shell.Current.GoToAsync(nameof(SubrosterListPage), new Dictionary<string, object>
        {
            ["rosterId"] = roster.Id,
            ["rosterName"] = roster.Name
        });
    }

    [RelayCommand]
    private static async Task OpenCalendarOverridesAsync(RosterDto roster)
    {
        await Shell.Current.GoToAsync("CalendarOverridesPage", new Dictionary<string, object>
        {
            ["rosterId"] = roster.Id
        });
    }
}
