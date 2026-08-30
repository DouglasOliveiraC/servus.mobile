using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.SwapRequests;

namespace Servus.Mobile.Features.Schedule;

[QueryProperty(nameof(SubrosterId), "subrosterId")]
public partial class SubrosterScheduleViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;
    private readonly ISwapRequestService _swapRequestService;

    private Guid? _myMemberId;
    private ScheduleAssignmentDto? _selectedMine;

    public SubrosterScheduleViewModel(IRosterService rosterService, ISwapRequestService swapRequestService)
    {
        _rosterService = rosterService;
        _swapRequestService = swapRequestService;
    }

    [ObservableProperty]
    private Guid subrosterId;

    [ObservableProperty]
    private string subrosterName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime toDate = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private string? summaryText;

    [ObservableProperty]
    private string? selectionHint;

    [ObservableProperty]
    private Guid selectedAssignmentId;

    public ObservableCollection<ScheduleDayDto> Days { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || SubrosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            _myMemberId = await _rosterService.GetMyMemberIdAsync();

            var from = DateOnly.FromDateTime(FromDate);
            var to = DateOnly.FromDateTime(ToDate);
            var schedule = await _rosterService.GetScheduleAsync(SubrosterId, from, to);

            SubrosterName = schedule.SubrosterName;

            Days.Clear();
            foreach (var day in schedule.Days.OrderBy(d => d.Date))
            {
                Days.Add(day);
            }

            var counts = await _rosterService.GetDutyCountsAsync(SubrosterId);
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
