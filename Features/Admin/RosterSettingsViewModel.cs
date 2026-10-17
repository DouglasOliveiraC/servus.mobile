using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Common;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(RosterId), "rosterId")]
public partial class RosterSettingsViewModel : BaseViewModel
{
    private readonly IRosterService _rosterService;

    public RosterSettingsViewModel(IRosterService rosterService, INavigationService navigationService, ITelemetryService telemetry)
        : base(navigationService, telemetry)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    public partial Guid RosterId { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MinIntervalDays { get; set; } = "0";

    [ObservableProperty]
    public partial bool RequiresAdminApproval { get; set; }

    [ObservableProperty]
    public partial string MinDaysAfterReturnBeforeEligible { get; set; } = "0";

    [ObservableProperty]
    public partial string MinDaysBeforeLeaveBlocked { get; set; } = "0";

    [ObservableProperty]
    public partial bool FillShiftsEarliestFirst { get; set; } = true;

    [ObservableProperty]
    public partial TimeSpan ServiceDayStartTime { get; set; } = new(8, 0, 0);

    /// <summary>Read-only summary — WorkdayShiftSize/RestDayShiftSize are now derived from
    /// the shifts below, shown here so the admin can see the total without adding them up.</summary>
    [ObservableProperty]
    public partial int WorkdayShiftSize { get; set; }

    [ObservableProperty]
    public partial int RestDayShiftSize { get; set; }

    public ObservableCollection<ServiceShiftEditRow> Shifts { get; } = [];

    /// <summary>No UI exposes these yet — round-tripped as loaded so saving other fields
    /// doesn't silently reset a roster's existing queue direction back to the default.</summary>
    private QueueDirection _workdayQueueDirection = QueueDirection.TopToBottom;
    private QueueDirection _weekendQueueDirection = QueueDirection.TopToBottom;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || RosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var roster = await _rosterService.GetRosterByIdAsync(RosterId);
            Name = roster.Name;
            MinIntervalDays = roster.MinIntervalDays.ToString();
            WorkdayShiftSize = roster.WorkdayShiftSize;
            RestDayShiftSize = roster.RestDayShiftSize;
            RequiresAdminApproval = roster.RequiresAdminApproval;
            MinDaysAfterReturnBeforeEligible = roster.MinDaysAfterReturnBeforeEligible.ToString();
            MinDaysBeforeLeaveBlocked = roster.MinDaysBeforeLeaveBlocked.ToString();
            FillShiftsEarliestFirst = roster.FillShiftsEarliestFirst;
            ServiceDayStartTime = roster.ServiceDayStartTime.ToTimeSpan();
            _workdayQueueDirection = roster.WorkdayQueueDirection;
            _weekendQueueDirection = roster.WeekendQueueDirection;

            Shifts.Clear();
            foreach (var shift in roster.ServiceShifts.OrderBy(s => s.SortOrder))
            {
                Shifts.Add(new ServiceShiftEditRow(shift));
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
    private void AddShift()
    {
        var lastEnd = Shifts.Count > 0 ? Shifts[^1].EndTime : ServiceDayStartTime;
        Shifts.Add(new ServiceShiftEditRow(lastEnd, lastEnd.Add(TimeSpan.FromHours(4)), workdayCapacity: 1, restDayCapacity: 1));
    }

    [RelayCommand]
    private void RemoveShift(ServiceShiftEditRow shift) => Shifts.Remove(shift);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        var minInterval = int.TryParse(MinIntervalDays, out var mi) ? mi : 0;
        var minDaysAfterReturn = int.TryParse(MinDaysAfterReturnBeforeEligible, out var mdr) ? mdr : 0;
        var minDaysBeforeLeave = int.TryParse(MinDaysBeforeLeaveBlocked, out var mdl) ? mdl : 0;
        var serviceDayStartTime = TimeOnly.FromTimeSpan(ServiceDayStartTime);

        // Full replace: an admin who opened this screen sees and can edit every shift, so an
        // empty Shifts list here really does mean "remove all shifts", not "leave untouched"
        // (that's what a null list would mean instead, but RosterSettingsPage always shows
        // the current set already loaded).
        var shifts = Shifts.Select(s => s.ToInputDto()).ToList();

        try
        {
            await _rosterService.UpdateRosterAsync(RosterId, Name.Trim(), minInterval, RequiresAdminApproval, minDaysAfterReturn, minDaysBeforeLeave, FillShiftsEarliestFirst, serviceDayStartTime, _workdayQueueDirection, _weekendQueueDirection, shifts);
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync("Configurações", "Alterações salvas.", "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível salvar as alterações.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteRosterAsync()
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir escala",
            $"Esta ação remove todos os vínculos e acessos de membros com \"{Name}\". Os membros continuam cadastrados no sistema, e o histórico de plantões é preservado.\n\nDigite \"{Name}\" para confirmar:");

        if (typed?.Trim() != Name) return;

        try
        {
            await _rosterService.DeleteRosterAsync(RosterId);
            await NavigationService.GoBackAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível excluir a escala.", "OK");
        }
    }
}

/// <summary>Editable row backing one ServiceShift in the settings screen's list — TimePicker
/// binds to TimeSpan, not TimeOnly, hence the conversion at the edges (ToInputDto).</summary>
public partial class ServiceShiftEditRow : ObservableObject
{
    public ServiceShiftEditRow(ServiceShiftDto dto)
        : this(dto.StartTime.ToTimeSpan(), dto.EndTime.ToTimeSpan(), dto.WorkdayCapacity, dto.RestDayCapacity)
    {
    }

    public ServiceShiftEditRow(TimeSpan startTime, TimeSpan endTime, int workdayCapacity, int restDayCapacity)
    {
        StartTime = startTime;
        EndTime = endTime;
        WorkdayCapacity = workdayCapacity.ToString();
        RestDayCapacity = restDayCapacity.ToString();
    }

    [ObservableProperty]
    public partial TimeSpan StartTime { get; set; }

    [ObservableProperty]
    public partial TimeSpan EndTime { get; set; }

    [ObservableProperty]
    public partial string WorkdayCapacity { get; set; } = "1";

    [ObservableProperty]
    public partial string RestDayCapacity { get; set; } = "1";

    public ServiceShiftInputDto ToInputDto() => new(
        TimeOnly.FromTimeSpan(StartTime),
        TimeOnly.FromTimeSpan(EndTime),
        int.TryParse(WorkdayCapacity, out var wc) ? wc : 0,
        int.TryParse(RestDayCapacity, out var rc) ? rc : 0);
}
