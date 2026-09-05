using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;
using Servus.Mobile.Services.Unavailabilities;

namespace Servus.Mobile.Features.Unavailability;

public partial class UnavailabilityViewModel : BaseViewModel
{
    private readonly IMemberUnavailabilityService _unavailabilityService;
    private readonly IRosterService _rosterService;

    private Guid? _myMemberId;

    public UnavailabilityViewModel(
        IMemberUnavailabilityService unavailabilityService,
        IRosterService rosterService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _unavailabilityService = unavailabilityService;
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool HasNoMember { get; set; }

    [ObservableProperty]
    public partial DateTime NewStartDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime NewEndDate { get; set; } = DateTime.Today;

    public ObservableCollection<MemberUnavailabilityDto> Periods { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            _myMemberId = await _rosterService.GetMyMemberIdAsync();
            if (_myMemberId is null)
            {
                HasNoMember = true;
                Periods.Clear();
                return;
            }

            HasNoMember = false;
            var periods = await _unavailabilityService.GetByMemberIdAsync(_myMemberId.Value);

            Periods.Clear();
            foreach (var period in periods.OrderByDescending(p => p.StartDate))
            {
                Periods.Add(period);
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
    private async Task AddAsync()
    {
        if (_myMemberId is null) return;

        var start = DateOnly.FromDateTime(NewStartDate);
        var end = DateOnly.FromDateTime(NewEndDate);
        if (end < start)
        {
            await Shell.Current.DisplayAlertAsync("Datas inválidas", "A data final não pode ser anterior à data inicial.", "OK");
            return;
        }

        try
        {
            var result = await _unavailabilityService.CreateAsync(_myMemberId.Value, start, end);
            await LoadAsync();

            // O período cadastrado pelo próprio membro não vale de imediato: sem dizer isso
            // aqui, ele sairia da tela achando que já está liberado daqueles dias.
            if (result.Status == MemberUnavailabilityStatus.Pending)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Solicitação enviada",
                    "Seu período foi registrado e passa a valer para a escala assim que um administrador aprovar.",
                    "OK");
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível registrar o período de indisponibilidade.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(MemberUnavailabilityDto period)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Remover período", "Remover este período de indisponibilidade?", "Remover", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _unavailabilityService.DeleteAsync(period.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível remover o período.", "OK");
        }
    }
}
