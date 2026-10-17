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

namespace Servus.Mobile.Features.Schedule;

[QueryProperty(nameof(RosterId), "rosterId")]
public partial class RosterScheduleViewModel : BaseViewModel, IRecipient<SwapUpdatedMessage>, IRecipient<ScheduleUpdatedMessage>
{
    private readonly IRosterService _rosterService;
    private readonly ISwapRequestService _swapRequestService;

    private Guid? _myMemberId;
    private ScheduleAssignmentDto? _selectedMine;

    public RosterScheduleViewModel(
        IRosterService rosterService,
        ISwapRequestService swapRequestService,
        INavigationService navigationService,
        ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _rosterService = rosterService;
        _swapRequestService = swapRequestService;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(SwapUpdatedMessage message)
    {
        if (message.Update.RosterId == RosterId)
        {
            MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync());
        }
    }

    public void Receive(ScheduleUpdatedMessage message)
    {
        if (message.Update.RosterId == RosterId)
        {
            MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync());
        }
    }

    [ObservableProperty]
    public partial Guid RosterId { get; set; }

    [ObservableProperty]
    public partial string RosterName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool CanManage { get; set; }

    [ObservableProperty]
    public partial DateTime FromDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime ToDate { get; set; } = DateTime.Today.AddDays(30);

    [ObservableProperty]
    public partial string? SummaryText { get; set; }

    [ObservableProperty]
    public partial string? PublishResultMessage { get; set; }

    [ObservableProperty]
    public partial string? SelectionHint { get; set; }

    [ObservableProperty]
    public partial Guid SelectedAssignmentId { get; set; }

    public ObservableCollection<ScheduleDayDto> Days { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || RosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            _myMemberId = await _rosterService.GetMyMemberIdAsync();

            var roster = await _rosterService.GetRosterByIdAsync(RosterId);
            CanManage = roster.CanManage;

            var from = DateOnly.FromDateTime(FromDate);
            var to = DateOnly.FromDateTime(ToDate);
            var schedule = await _rosterService.GetScheduleAsync(RosterId, from, to);

            RosterName = schedule.RosterName;

            Days.Clear();
            foreach (var day in schedule.Days.OrderBy(d => d.Date))
            {
                Days.Add(day);
            }

            var counts = await _rosterService.GetDutyCountsAsync(RosterId);
            var totalAssignments = counts.Sum(c => c.AssignmentCount);
            SummaryText = counts.Count > 0
                ? $"{totalAssignments} plantão(ões) distribuídos entre {counts.Count} membro(s)"
                : null;
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
    private async Task PublishScheduleAsync()
    {
        try
        {
            var from = DateOnly.FromDateTime(FromDate);
            var to = DateOnly.FromDateTime(ToDate);
            var result = await _rosterService.PublishScheduleAsync(RosterId, from, to);
            PublishResultMessage = $"{result.AssignmentsPublished} plantão(ões) publicado(s).";
            await LoadAsync();
        }
        catch (Exception)
        {
            PublishResultMessage = null;
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível publicar a escala.", "OK");
        }
    }

    [RelayCommand]
    private async Task SelectAssignmentAsync(ScheduleAssignmentDto assignment)
    {
        if (_myMemberId is not null && assignment.MemberId == _myMemberId)
        {
            _selectedMine = assignment;
            SelectedAssignmentId = assignment.DutyAssignmentId;
            SelectionHint = "Toque no plantão de outro membro para propor a troca.";
            return;
        }

        if (_selectedMine is null)
        {
            SelectionHint = "Selecione primeiro um dos seus plantões para propor uma troca.";
            return;
        }

        if (_myMemberId is null) return;

        try
        {
            await _swapRequestService.RequestSwapAsync(_selectedMine.DutyAssignmentId, assignment.DutyAssignmentId, _myMemberId.Value);
            SelectionHint = "Solicitação de troca enviada.";
            _selectedMine = null;
            SelectedAssignmentId = Guid.Empty;
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível solicitar a troca.", "OK");
            _selectedMine = null;
            SelectedAssignmentId = Guid.Empty;
            SelectionHint = null;
        }
    }
}
