using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Servus.Mobile.Services.CalendarOverrides;

namespace Servus.Mobile.Features.CalendarOverrides;

[QueryProperty(nameof(RosterId), "rosterId")]
public partial class CalendarOverridesViewModel : ObservableObject
{
    private readonly ICalendarDayOverrideService _overrideService;

    public CalendarOverridesViewModel(ICalendarDayOverrideService overrideService)
    {
        _overrideService = overrideService;
    }

    [ObservableProperty]
    private Guid rosterId;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private DateTime rangeFrom = DateTime.Today;

    [ObservableProperty]
    private DateTime rangeTo = DateTime.Today.AddDays(60);

    [ObservableProperty]
    private DateTime newDate = DateTime.Today;

    [ObservableProperty]
    private bool newIsRestDay = true;

    public ObservableCollection<CalendarDayOverrideDto> Overrides { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || RosterId == Guid.Empty) return;

        IsBusy = true;
        HasError = false;
        try
        {
            var from = DateOnly.FromDateTime(RangeFrom);
            var to = DateOnly.FromDateTime(RangeTo);
            var overrides = await _overrideService.GetByRangeAsync(RosterId, from, to);

            Overrides.Clear();
            foreach (var item in overrides.OrderBy(o => o.Date))
            {
                Overrides.Add(item);
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
        try
        {
            await _overrideService.CreateAsync(RosterId, DateOnly.FromDateTime(NewDate), NewIsRestDay);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível adicionar o dia especial.", "OK");
        }
    }

    [RelayCommand]
    private async Task ToggleRestDayAsync(CalendarDayOverrideDto item)
    {
        try
        {
            await _overrideService.UpdateAsync(item.Id, !item.IsRestDay);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível atualizar o dia especial.", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(CalendarDayOverrideDto item)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Remover dia especial", "Remover este dia especial?", "Remover", "Cancelar");
        if (!confirmed) return;

        try
        {
            await _overrideService.DeleteAsync(item.Id);
            await LoadAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível remover o dia especial.", "OK");
        }
    }
}
