using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.Rosters;

namespace Servus.Mobile.Features.Admin;

[QueryProperty(nameof(SubrosterId), "subrosterId")]
public partial class SubrosterSettingsViewModel : ObservableObject
{
    private readonly IRosterService _rosterService;

    public SubrosterSettingsViewModel(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [ObservableProperty]
    private Guid subrosterId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string minIntervalDays = "0";

    [ObservableProperty]
    private bool requiresAdminApproval;

    [ObservableProperty]
    private string minDaysAfterReturnBeforeEligible = "0";

    [ObservableProperty]
    private bool fillShiftsEarliestFirst = true;

    [ObservableProperty]
    private TimeSpan serviceDayStartTime = new(8, 0, 0);

    /// <summary>Read-only summary — WorkdayShiftSize/RestDayShiftSize are now derived from
    /// the shifts below, shown here so the admin can see the total without adding them up.</summary>
    [ObservableProperty]
    private int workdayShiftSize;

    [ObservableProperty]
    private int restDayShiftSize;

    public ObservableCollection<ServiceShiftEditRow> Shifts { get; } = [];

    private bool _isLoading;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || SubrosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        _isLoading = true;
        try
        {
            var subroster = await _rosterService.GetSubrosterByIdAsync(SubrosterId);
            Name = subroster.Name;
            MinIntervalDays = subroster.MinIntervalDays.ToString();
            WorkdayShiftSize = subroster.WorkdayShiftSize;
            RestDayShiftSize = subroster.RestDayShiftSize;
            RequiresAdminApproval = subroster.RequiresAdminApproval;
            MinDaysAfterReturnBeforeEligible = subroster.MinDaysAfterReturnBeforeEligible.ToString();
            FillShiftsEarliestFirst = subroster.FillShiftsEarliestFirst;
            ServiceDayStartTime = subroster.ServiceDayStartTime.ToTimeSpan();

            Shifts.Clear();
            foreach (var shift in subroster.ServiceShifts.OrderBy(s => s.SortOrder))
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
            _isLoading = false;
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
        var serviceDayStartTime = TimeOnly.FromTimeSpan(ServiceDayStartTime);

        // Full replace: an admin who opened this screen sees and can edit every shift, so an
        // empty Shifts list here really does mean "remove all shifts", not "leave untouched"
        // (that's what a null list would mean instead, but SubrosterSettingsPage always shows
        // the current set already loaded).
        var shifts = Shifts.Select(s => s.ToInputDto()).ToList();

        try
        {
            await _rosterService.UpdateSubrosterAsync(SubrosterId, Name.Trim(), minInterval, RequiresAdminApproval, minDaysAfterReturn, FillShiftsEarliestFirst, serviceDayStartTime, shifts);
            await LoadAsync();
            await Shell.Current.DisplayAlertAsync("Configurações", "Alterações salvas.", "OK");
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível salvar as alterações.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteSubrosterAsync()
    {
        var typed = await Shell.Current.DisplayPromptAsync(
            "Excluir escala",
            $"Esta ação remove todos os vínculos e acessos de membros com \"{Name}\" e não pode ser desfeita. Os membros continuam cadastrados no sistema.\n\nDigite \"{Name}\" para confirmar:");

        if (typed?.Trim() != Name) return;

        try
        {
            await _rosterService.DeleteSubrosterAsync(SubrosterId);
            await Shell.Current.GoToAsync("..");
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
    private TimeSpan startTime;

    [ObservableProperty]
    private TimeSpan endTime;

    [ObservableProperty]
    private string workdayCapacity = "1";

    [ObservableProperty]
    private string restDayCapacity = "1";

    public ServiceShiftInputDto ToInputDto() => new(
        TimeOnly.FromTimeSpan(StartTime),
        TimeOnly.FromTimeSpan(EndTime),
        int.TryParse(WorkdayCapacity, out var wc) ? wc : 0,
        int.TryParse(RestDayCapacity, out var rc) ? rc : 0);
}
