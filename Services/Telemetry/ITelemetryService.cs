namespace Servus.Mobile.Services.Telemetry;

public interface ITelemetryService
{
    IDisposable? StartOperation(string name);

    /// <summary>
    /// Publica um evento de uso. O resultado é tipado porque vários serviços do app devolvem
    /// <c>Success=false</c> sem lançar: tratar "não lançou" como sucesso escondia justamente
    /// as falhas de negócio que este evento existe para contar (R5 no relatório 10).
    /// </summary>
    void TrackEvent(
        string name,
        ClientOutcome outcome,
        TimeSpan elapsed,
        string? reasonCode = null,
        IReadOnlyDictionary<string, string>? extra = null);

    void TrackException(Exception exception);
}
