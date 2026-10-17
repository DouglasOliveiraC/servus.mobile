using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Realtime;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.SwapRequests;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Features.Swaps;

public partial class SwapRequestsViewModel : BaseViewModel, IRecipient<SwapUpdatedMessage>
{
    private readonly ISwapRequestService _swapRequestService;
    private readonly IRosterService _rosterService;

    private Guid _myMemberId;

    public SwapRequestsViewModel(
        ISwapRequestService swapRequestService,
        IRosterService rosterService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _swapRequestService = swapRequestService;
        _rosterService = rosterService;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    // Any swap event (requested/responded/approved/cancelled/expired) invalidates both lists
    // alike — simpler and safer than deciding, from the received Status alone, whether the
    // change affects "awaiting my response" or "requested by me".
    public void Receive(SwapUpdatedMessage message) => MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync());

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    public ObservableCollection<SwapRequestDto> AwaitingMyResponse { get; } = [];

    public ObservableCollection<SwapRequestDto> RequestedByMe { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var myMemberId = await _rosterService.GetMyMemberIdAsync();
            if (myMemberId is null)
            {
                AwaitingMyResponse.Clear();
                RequestedByMe.Clear();
                return;
            }

            _myMemberId = myMemberId.Value;
            var pending = await _swapRequestService.GetPendingAsync(_myMemberId);
            var manageableRosterIds = await GetManageableRosterIdsAsync(pending);

            AwaitingMyResponse.Clear();
            RequestedByMe.Clear();

            foreach (var request in pending)
            {
                if (request.TargetMemberId == _myMemberId && request.Status == SwapRequestStatusDto.Pending)
                {
                    AwaitingMyResponse.Add(request);
                }
                else if (request.RequestingMemberId == _myMemberId)
                {
                    RequestedByMe.Add(request with
                    {
                        CanApprove = request.Status == SwapRequestStatusDto.AwaitingAdminApproval
                            && manageableRosterIds.Contains(request.RosterId)
                    });
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

    // Only rosters with a request waiting for approval are looked up. CanManage means
    // organization admin, a subset of who the API lets approve (a full roster admin also can),
    // so such a roster admin does not get the button here.
    private async Task<HashSet<Guid>> GetManageableRosterIdsAsync(IEnumerable<SwapRequestDto> requests)
    {
        var rosterIds = requests
            .Where(r => r.Status == SwapRequestStatusDto.AwaitingAdminApproval)
            .Select(r => r.RosterId)
            .Distinct();

        var manageable = new HashSet<Guid>();
        foreach (var rosterId in rosterIds)
        {
            var roster = await _rosterService.GetRosterByIdAsync(rosterId);
            if (roster.CanManage)
            {
                manageable.Add(rosterId);
            }
        }

        return manageable;
    }

    [RelayCommand]
    private async Task AcceptAsync(SwapRequestDto request)
    {
        try
        {
            await _swapRequestService.RespondAsync(request.Id, _myMemberId, accept: true);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível aceitar a troca.", "OK");
        }
    }

    [RelayCommand]
    private async Task RejectAsync(SwapRequestDto request)
    {
        try
        {
            await _swapRequestService.RespondAsync(request.Id, _myMemberId, accept: false);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível recusar a troca.", "OK");
        }
    }

    [RelayCommand]
    private async Task CancelAsync(SwapRequestDto request)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Cancelar troca", "Cancelar esta solicitação de troca?", "Cancelar troca", "Voltar");
        if (!confirmed) return;

        try
        {
            await _swapRequestService.CancelAsync(request.Id, _myMemberId);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível cancelar a troca.", "OK");
        }
    }

    [RelayCommand]
    private async Task ApproveAsync(SwapRequestDto request)
    {
        try
        {
            await _swapRequestService.ApproveAsync(request.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Você não tem permissão para aprovar esta troca ou ela não pode mais ser aprovada.", "OK");
        }
    }
}
