using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Common;

public abstract partial class BaseViewModel : ObservableObject
{
    protected readonly INavigationService NavigationService;
    private readonly ITelemetryService _telemetry;

    protected BaseViewModel(INavigationService navigationService, ITelemetryService telemetry)
    {
        NavigationService = navigationService;
        _telemetry = telemetry;
    }

    // Mede a duração e envia um evento de sucesso/falha — não engole a exceção:
    // cada ViewModel já tem try/catch próprio com mensagens de erro específicas
    // (HasError, PasswordMessage, etc.), que precisa continuar funcionando como
    // hoje. Chame isso DENTRO do try/catch existente do call site.
    protected async Task RunTrackedAsync(string operationName, Func<Task> action)
    {
        using var _ = _telemetry.StartOperation(operationName);
        var sw = Stopwatch.StartNew();
        var success = true;
        try
        {
            await action();
        }
        catch
        {
            success = false;
            throw;
        }
        finally
        {
            _telemetry.TrackEvent(operationName, success, sw.Elapsed);
        }
    }
}
