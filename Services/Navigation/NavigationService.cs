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

    /// <remarks>
    /// O destino é DIMENSÃO, não parte do nome do evento. Antes o nome era
    /// "Navigate:{rota}", o que criava um evento distinto por rota — e por rota com id, um
    /// evento distinto por escala visitada. Com o nome fixo, "quais telas são usadas" vira uma
    /// agregação por Screen em vez de uma lista de nomes que ninguém consegue consultar junto.
    ///
    /// Mede o término do GoToAsync, que é quando a tela aparece — não quando ela tem dados. O
    /// tempo até ficar útil é o screen.ready, que só a própria tela sabe emitir.
    /// </remarks>
    private async Task NavigateAsync(string destination, Func<Task> navigate)
    {
        using var _ = _telemetry.StartOperation("screen.navigate");
        var sw = Stopwatch.StartNew();
        var screen = new Dictionary<string, string> { ["Screen"] = destination };

        try
        {
            await navigate();
            _telemetry.TrackEvent(ClientEventNames.ScreenViewed, ClientOutcome.Succeeded, sw.Elapsed, extra: screen);
        }
        catch (Exception ex)
        {
            _telemetry.TrackEvent(ClientEventNames.ScreenViewed, ClientOutcome.Failed, sw.Elapsed, extra: screen);
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
