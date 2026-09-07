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
    //
    // A operação vai como dimensão Action, e não no nome do evento: o nome precisa ser fixo
    // para que "quais ações os usuários concluem, e em quanto tempo" seja uma agregação
    // única em vez de um evento novo a cada tela instrumentada.
    protected async Task RunTrackedAsync(string operationName, Func<Task> action)
    {
        using var _ = _telemetry.StartOperation(operationName);
        var sw = Stopwatch.StartNew();
        var outcome = ClientOutcome.Succeeded;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // Cancelamento não é falha: quem sai da tela antes de carregar não deve entrar na
            // taxa de erro do app.
            outcome = ClientOutcome.Cancelled;
            throw;
        }
        catch
        {
            outcome = ClientOutcome.Failed;
            throw;
        }
        finally
        {
            _telemetry.TrackEvent(
                ClientEventNames.UiActionCompleted,
                outcome,
                sw.Elapsed,
                extra: new Dictionary<string, string> { ["Action"] = operationName });
        }
    }

    /// <summary>
    /// Marca a tela como pronta PARA USO — dados carregados e renderizáveis, não apenas
    /// navegada. É a diferença entre "a tela abriu" e "a pessoa conseguiu fazer algo".
    /// </summary>
    protected void TrackScreenReady(string screen, TimeSpan elapsed, ClientOutcome outcome = ClientOutcome.Succeeded)
    {
        _telemetry.TrackEvent(
            ClientEventNames.ScreenReady,
            outcome,
            elapsed,
            extra: new Dictionary<string, string> { ["Screen"] = screen });
    }
}
