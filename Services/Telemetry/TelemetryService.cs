using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace Servus.Mobile.Services.Telemetry;

public class TelemetryService : ITelemetryService
{
    // Fonte única de spans do app inteiro — o nome precisa bater com o
    // AddSource("Servus.Mobile") registrado no TracerProvider (MauiProgram.cs).
    private static readonly ActivitySource ActivitySource = new("Servus.Mobile");

    private readonly ILogger<TelemetryService>? _logger;

    // _logger é null quando a connection string não está configurada (ver
    // MauiProgram.ConfigureTelemetry) — todo método abaixo já no-opa nesse caso.
    public TelemetryService(ILogger<TelemetryService>? logger = null)
    {
        _logger = logger;
    }

    public IDisposable? StartOperation(string name) => ActivitySource.StartActivity(name, ActivityKind.Internal);

    public void TrackEvent(string name, bool success, TimeSpan elapsed)
    {
        // Nome do evento customizado no Application Insights: só populam a
        // tabela customEvents (aba "Events") logs com essa chave de template
        // específica — não é convenção nossa, é o contrato do exporter do Azure
        // Monitor para OpenTelemetry.
        _logger?.LogInformation(
            "{microsoft.custom_event.name} {Success} {ElapsedMs}",
            name, success, elapsed.TotalMilliseconds);
    }

    public void TrackException(Exception exception)
    {
        var activity = Activity.Current;
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.AddException(exception);
        _logger?.LogError(exception, "Unhandled exception tracked");
    }
}
