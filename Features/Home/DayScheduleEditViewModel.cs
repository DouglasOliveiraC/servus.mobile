using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;
using MemberDto = Servus.Mobile.Services.Members.MemberDto;

namespace Servus.Mobile.Features.Home;

/// <summary>
/// Lets an admin swap who is scheduled on one specific day of one Roster — reuses the
/// admin-direct swap endpoint (PUT api/duty-assignment/{id}/swap), which unlike the peer
/// SwapRequest flow needs no negotiation/approval. The candidate list for "who can this
/// slot go to" is the Roster's own queue (fetched once here, filtered in memory as the
/// admin types) rather than every Member in the system, matching the backend's own new
/// validation that the replacement must belong to this Roster's queue.
/// </summary>
[QueryProperty(nameof(RosterId), "rosterId")]
[QueryProperty(nameof(DateText), "date")]
public partial class DayScheduleEditViewModel : BaseViewModel
{
    private readonly IRosterService _rosterService;

    private IReadOnlyList<RosterMemberDto> _queue = [];
    private IReadOnlyList<ServiceShiftDto> _shifts = [];

    public DayScheduleEditViewModel(IRosterService rosterService, INavigationService navigationService, ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial Guid RosterId { get; set; }

    [ObservableProperty]
    public partial string DateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateOnly Date { get; set; }

    [ObservableProperty]
    public partial string DateLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RosterName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial bool IsSwapFormVisible { get; set; }

    [ObservableProperty]
    public partial ScheduleAssignmentDto? SwapTarget { get; set; }

    [ObservableProperty]
    public partial string MemberSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SwapWarningMessage { get; set; }

    public ObservableCollection<ScheduleAssignmentDto> Assignments { get; } = [];

    /// <summary>Assignments grouped by ServiceShift, ordered chronologically — what the page
    /// actually displays, so "which shift is this person in" is visible at a glance.</summary>
    public ObservableCollection<ShiftGroup> ShiftGroups { get; } = [];

    public ObservableCollection<MemberDto> MemberSearchResults { get; } = [];

    [ObservableProperty]
    public partial bool IsMoveShiftFormVisible { get; set; }

    [ObservableProperty]
    public partial ScheduleAssignmentDto? MoveShiftTarget { get; set; }

    public ObservableCollection<ServiceShiftDto> AvailableShiftsForMove { get; } = [];

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
        if (IsBusy || RosterId == Guid.Empty || Date == default) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var schedule = await _rosterService.GetScheduleAsync(RosterId, Date, Date);
            RosterName = schedule.RosterName;

            var day = schedule.Days.FirstOrDefault(d => d.Date == Date);
            Assignments.Clear();
            foreach (var assignment in day?.Assignments ?? [])
            {
                Assignments.Add(assignment);
            }

            var roster = await _rosterService.GetRosterByIdAsync(RosterId);
            _shifts = roster.ServiceShifts.OrderBy(s => s.SortOrder).ToList();

            ShiftGroups.Clear();
            foreach (var shift in _shifts)
            {
                var shiftAssignments = Assignments.Where(a => a.ServiceShiftId == shift.Id).ToList();
                ShiftGroups.Add(new ShiftGroup(shift, shiftAssignments));
            }

            _queue = await _rosterService.GetRosterMembersAsync(RosterId);
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
    private void StartSwap(ScheduleAssignmentDto assignment)
    {
        SwapTarget = assignment;
        IsSwapFormVisible = true;
        MemberSearchText = string.Empty;
        SwapWarningMessage = null;
        RefreshMemberSearchResults();
    }

    [RelayCommand]
    private void CancelSwap()
    {
        IsSwapFormVisible = false;
        SwapTarget = null;
        MemberSearchResults.Clear();
    }

    partial void OnMemberSearchTextChanged(string value) => RefreshMemberSearchResults();

    // The queue is small and already fully loaded (see class remarks), so filtering as the
    // admin types is instant in-memory work — no per-keystroke network round trip.
    private void RefreshMemberSearchResults()
    {
        MemberSearchResults.Clear();
        if (SwapTarget is null) return;

        var alreadyAssignedToday = Assignments.Select(a => a.MemberId).ToHashSet();
        var search = MemberSearchText.Trim();

        var matches = _queue
            .Where(sm => !alreadyAssignedToday.Contains(sm.MemberId))
            .Where(sm => search.Length == 0 || sm.MemberName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(sm => sm.MemberName);

        foreach (var member in matches)
        {
            MemberSearchResults.Add(new MemberDto(member.MemberId, member.MemberName, null, null, null, default));
        }
    }

    [RelayCommand]
    private async Task ConfirmSwapAsync(MemberDto newMember)
    {
        if (SwapTarget is null) return;

        try
        {
            var result = await _rosterService.SwapDutyAssignmentAsync(SwapTarget.DutyAssignmentId, newMember.Id);
            SwapWarningMessage = result.MinIntervalWarning ? result.MinIntervalWarningMessage : null;
            IsSwapFormVisible = false;
            SwapTarget = null;
            MemberSearchResults.Clear();
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível trocar o membro escalado.", "OK");
        }
    }

    [RelayCommand]
    private void StartMoveShift(ScheduleAssignmentDto assignment)
    {
        MoveShiftTarget = assignment;
        IsMoveShiftFormVisible = true;

        AvailableShiftsForMove.Clear();
        foreach (var shift in _shifts.Where(s => s.Id != assignment.ServiceShiftId))
        {
            AvailableShiftsForMove.Add(shift);
        }
    }

    [RelayCommand]
    private void CancelMoveShift()
    {
        IsMoveShiftFormVisible = false;
        MoveShiftTarget = null;
        AvailableShiftsForMove.Clear();
    }

    [RelayCommand]
    private async Task ConfirmMoveShiftAsync(ServiceShiftDto targetShift)
    {
        if (MoveShiftTarget is null) return;

        try
        {
            await _rosterService.MoveDutyAssignmentToShiftAsync(MoveShiftTarget.DutyAssignmentId, targetShift.Id);
            IsMoveShiftFormVisible = false;
            MoveShiftTarget = null;
            AvailableShiftsForMove.Clear();
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível mover para este turno — pode estar sem vaga.", "OK");
        }
    }
}

/// <summary>One ServiceShift and who occupies it on the day being edited.</summary>
public record ShiftGroup(ServiceShiftDto Shift, IReadOnlyList<ScheduleAssignmentDto> Assignments);
