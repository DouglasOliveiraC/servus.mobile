namespace Servus.Mobile.Services.Telemetry;

public interface ITelemetryService
{
    IDisposable? StartOperation(string name);

    void TrackEvent(string name, bool success, TimeSpan elapsed);

    void TrackException(Exception exception);
}
