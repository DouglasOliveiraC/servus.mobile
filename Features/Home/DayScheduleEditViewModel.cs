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

    // The row whose inline form is open. Forms open under their own row instead of at the end
    // of the page: on a day with many people a form at the bottom opened off-screen, so tapping
    // Trocar/Mover looked like it did nothing.
    private AssignmentRow? _activeRow;

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    // Mover only makes sense with somewhere else to go; with a single shift the button opened
    // an empty form that could only be cancelled.
    [ObservableProperty]
    public partial bool HasMultipleShifts { get; set; }

    [ObservableProperty]
    public partial string MemberSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SwapWarningMessage { get; set; }

    public ObservableCollection<AssignmentRow> Assignments { get; } = [];

    /// <summary>Assignments grouped by ServiceShift, ordered chronologically — what the page
    /// actually displays, so "which shift is this person in" is visible at a glance.</summary>
    public ObservableCollection<ShiftGroup> ShiftGroups { get; } = [];

    public ObservableCollection<MemberDto> MemberSearchResults { get; } = [];

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
            _activeRow = null;
            Assignments.Clear();
            foreach (var assignment in day?.Assignments ?? [])
            {
                Assignments.Add(new AssignmentRow(assignment));
            }

            var roster = await _rosterService.GetRosterByIdAsync(RosterId);
            _shifts = roster.ServiceShifts.OrderBy(s => s.SortOrder).ToList();
            HasMultipleShifts = _shifts.Count > 1;

            ShiftGroups.Clear();
            foreach (var shift in _shifts)
            {
                var shiftAssignments = Assignments.Where(a => a.Assignment.ServiceShiftId == shift.Id).ToList();
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
    private void StartSwap(AssignmentRow row)
    {
        var reopening = _activeRow == row && row.IsSwapping;
        CloseActiveForm();
        if (reopening) return;

        _activeRow = row;
        row.IsSwapping = true;
        MemberSearchText = string.Empty;
        SwapWarningMessage = null;
        RefreshMemberSearchResults();
    }

    [RelayCommand]
    private void StartMoveShift(AssignmentRow row)
    {
        var reopening = _activeRow == row && row.IsMoving;
        CloseActiveForm();
        if (reopening) return;

        _activeRow = row;
        row.IsMoving = true;
        foreach (var shift in _shifts.Where(s => s.Id != row.Assignment.ServiceShiftId))
        {
            AvailableShiftsForMove.Add(shift);
        }
    }

    [RelayCommand]
    private void CancelForm() => CloseActiveForm();

    private void CloseActiveForm()
    {
        if (_activeRow is not null)
        {
            _activeRow.IsSwapping = false;
            _activeRow.IsMoving = false;
        }

        _activeRow = null;
        MemberSearchResults.Clear();
        AvailableShiftsForMove.Clear();
    }

    partial void OnMemberSearchTextChanged(string value) => RefreshMemberSearchResults();

    // The queue is small and already fully loaded (see class remarks), so filtering as the
    // admin types is instant in-memory work — no per-keystroke network round trip.
    private void RefreshMemberSearchResults()
    {
        MemberSearchResults.Clear();
        if (_activeRow is not { IsSwapping: true }) return;

        var alreadyAssignedToday = Assignments.Select(a => a.Assignment.MemberId).ToHashSet();
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
        if (_activeRow is not { IsSwapping: true } row || IsSaving) return;

        IsSaving = true;
        try
        {
            var result = await _rosterService.SwapDutyAssignmentAsync(row.Assignment.DutyAssignmentId, newMember.Id);
            SwapWarningMessage = result.MinIntervalWarning ? result.MinIntervalWarningMessage : null;
            CloseActiveForm();
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível trocar o membro escalado.", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmMoveShiftAsync(ServiceShiftDto targetShift)
    {
        if (_activeRow is not { IsMoving: true } row || IsSaving) return;

        IsSaving = true;
        try
        {
            await _rosterService.MoveDutyAssignmentToShiftAsync(row.Assignment.DutyAssignmentId, targetShift.Id);
            CloseActiveForm();
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível mover para este turno — pode estar sem vaga.", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }
}

/// <summary>One assignment on the edited day, plus which of its inline forms is open.</summary>
public partial class AssignmentRow(ScheduleAssignmentDto assignment) : ObservableObject
{
    public ScheduleAssignmentDto Assignment { get; } = assignment;

    public string MemberName => Assignment.MemberName;

    [ObservableProperty]
    public partial bool IsSwapping { get; set; }

    [ObservableProperty]
    public partial bool IsMoving { get; set; }
}

/// <summary>One ServiceShift and who occupies it on the day being edited.</summary>
public record ShiftGroup(ServiceShiftDto Shift, IReadOnlyList<AssignmentRow> Assignments);
