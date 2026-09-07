using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace Servus.Mobile.Services.Telemetry;

public class TelemetryService : ITelemetryService
{
    // Fonte única de spans do app inteiro — o nome precisa bater com o
    // AddSource("Servus.Mobile") registrado no TracerProvider (MauiProgram.cs).
    private static readonly ActivitySource ActivitySource = new("Servus.Mobile");

    // Identificador de sessão analítica: aleatório, gerado no app, sem relação com JWT ou
    // refresh token. É o que permite reconstruir uma jornada sem identificar a pessoa.
    private static readonly string SessionId = Guid.NewGuid().ToString("N");

    private readonly ILogger<TelemetryService>? _logger;

    // _logger é null quando a connection string não está configurada (ver
    // MauiProgram.ConfigureTelemetry) — todo método abaixo já no-opa nesse caso.
    public TelemetryService(ILogger<TelemetryService>? logger = null)
    {
        _logger = logger;
    }

    public IDisposable? StartOperation(string name) => ActivitySource.StartActivity(name, ActivityKind.Internal);

    public void TrackEvent(
        string name,
        ClientOutcome outcome,
        TimeSpan elapsed,
        string? reasonCode = null,
        IReadOnlyDictionary<string, string>? extra = null)
    {
        if (_logger is null)
        {
            return;
        }

        var scope = new Dictionary<string, object?>
        {
            ["Outcome"] = ToWireValue(outcome),
            ["DurationMs"] = elapsed.TotalMilliseconds,
            ["SessionId"] = SessionId,
            ["ClientPlatform"] = DeviceInfo.Platform.ToString(),
            ["AppVersion"] = AppInfo.VersionString
        };

        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            scope["ReasonCode"] = reasonCode;
        }

        if (extra is not null)
        {
            foreach (var (key, value) in extra)
            {
                scope[key] = value;
            }
        }

        using (_logger.BeginScope(scope))
        {
            // Nome do evento customizado no Application Insights: só populam a
            // tabela customEvents (aba "Events") logs com essa chave de template
            // específica — não é convenção nossa, é o contrato do exporter do Azure
            // Monitor para OpenTelemetry.
            _logger.LogInformation("{microsoft.custom_event.name}", name);
        }
    }

    public void TrackException(Exception exception)
    {
        var activity = Activity.Current;
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.AddException(exception);
        _logger?.LogError(exception, "Unhandled exception tracked");
    }

    private static string ToWireValue(ClientOutcome outcome) => outcome switch
    {
        ClientOutcome.Succeeded => "succeeded",
        ClientOutcome.Failed => "failed",
        ClientOutcome.Cancelled => "cancelled",
        _ => "unknown"
    };
}
