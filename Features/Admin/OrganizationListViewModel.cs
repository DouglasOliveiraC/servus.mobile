using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Organizations;

namespace Servus.Mobile.Features.Admin;

public partial class OrganizationListViewModel : ObservableObject
{
    private readonly IOrganizationService _organizationService;

    public OrganizationListViewModel(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    public ObservableCollection<OrganizationDto> Organizations { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var organizations = await _organizationService.GetMyOrganizationsAsync();
            Organizations.Clear();
            foreach (var organization in organizations)
            {
                Organizations.Add(organization);
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
    private async Task CreateOrganizationAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("Nova organização", "Nome da organização (ex.: hospital, batalhão, equipe):");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await _organizationService.CreateOrganizationAsync(name.Trim());
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível criar a organização.", "OK");
        }
    }

    [RelayCommand]
    private async Task RenameOrganizationAsync(OrganizationDto organization)
    {
        var name = await Shell.Current.DisplayPromptAsync("Renomear organização", "Novo nome:", initialValue: organization.Name);
        if (string.IsNullOrWhiteSpace(name) || name.Trim() == organization.Name) return;

        try
        {
            await _organizationService.UpdateOrganizationNameAsync(organization.Id, name.Trim());
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível renomear a organização.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteOrganizationAsync(OrganizationDto organization)
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir organização",
            $"Esta ação exclui também todas as escalas e remove os vínculos e acessos de membros com \"{organization.Name}\" e não pode ser desfeita. Os membros continuam cadastrados no sistema.\n\nDigite \"{organization.Name}\" para confirmar:");

        if (typed?.Trim() != organization.Name) return;

        try
        {
            await _organizationService.DeleteOrganizationAsync(organization.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a organização.", "OK");
        }
    }

    [RelayCommand]
    private static async Task OpenOrganizationAsync(OrganizationDto organization)
    {
        await Shell.Current.GoToAsync(nameof(RosterListPage), new Dictionary<string, object>
        {
            ["organizationId"] = organization.Id,
            ["organizationName"] = organization.Name
        });
    }

    [RelayCommand]
    private static async Task OpenCalendarOverridesAsync(OrganizationDto organization)
    {
        await Shell.Current.GoToAsync("CalendarOverridesPage", new Dictionary<string, object>
        {
            ["organizationId"] = organization.Id
        });
    }
}
