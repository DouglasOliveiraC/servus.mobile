using System.Diagnostics;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly ITelemetryService _telemetry;

    public NavigationService(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public Task GoToAsync(string route) => NavigateAsync(route, () => Shell.Current.GoToAsync(route));

    public Task GoToAsync(string route, IDictionary<string, object> parameters) =>
        NavigateAsync(route, () => Shell.Current.GoToAsync(route, parameters));

    public Task GoToRootAsync(string route) => NavigateAsync(route, () => Shell.Current.GoToAsync($"//{route}"));

    public Task GoBackAsync() => NavigateAsync("..", () => Shell.Current.GoToAsync(".."));

    private async Task NavigateAsync(string destination, Func<Task> navigate)
    {
        using var _ = _telemetry.StartOperation($"Navigate:{destination}");
        var sw = Stopwatch.StartNew();
        try
        {
            await navigate();
            _telemetry.TrackEvent($"Navigate:{destination}", success: true, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _telemetry.TrackEvent($"Navigate:{destination}", success: false, sw.Elapsed);
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
