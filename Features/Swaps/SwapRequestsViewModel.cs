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

    public ObservableCollection<SwapRequestDto> AwaitingMyApproval { get; } = [];

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
            // Loaded first and independently of the member: approving is an admin right, and the
            // swaps it covers are usually between two other people.
            var awaitingApproval = await _swapRequestService.GetAwaitingMyApprovalAsync();
            var approvalIds = awaitingApproval.Select(r => r.Id).ToHashSet();

            AwaitingMyApproval.Clear();
            foreach (var request in awaitingApproval)
            {
                AwaitingMyApproval.Add(request with { CanApprove = true });
            }

            var myMemberId = await _rosterService.GetMyMemberIdAsync();
            AwaitingMyResponse.Clear();
            RequestedByMe.Clear();
            if (myMemberId is null)
            {
                return;
            }

            _myMemberId = myMemberId.Value;
            var pending = await _swapRequestService.GetPendingAsync(_myMemberId);

            // A swap the user is a party to and can also approve is shown once, under approval.
            foreach (var request in pending.Where(r => !approvalIds.Contains(r.Id)))
            {
                if (request.TargetMemberId == _myMemberId && request.Status == SwapRequestStatusDto.Pending)
                {
                    AwaitingMyResponse.Add(request);
                }
                else if (request.RequestingMemberId == _myMemberId)
                {
                    RequestedByMe.Add(request);
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
