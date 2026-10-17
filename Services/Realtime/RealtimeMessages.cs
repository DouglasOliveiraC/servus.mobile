namespace Servus.Mobile.Services.Realtime;

/// <summary>Payload shapes mirror IRealtimeNotifier's notification records on the backend
/// (Servus.Application.Interfaces.RealtimeInterfaces) — kept as separate types here since the
/// mobile app has no reference to Servus.Application.</summary>
public record SwapRequestUpdate(Guid SwapRequestId, Guid RosterId, string Status);

// Common shape of SchedulePublished, ScheduleUnpublished and ScheduleChanged: every screen
// reacts the same way (reload the roster), so only the roster id is read.
public record ScheduleUpdate(Guid RosterId);

public sealed class SwapUpdatedMessage
{
    public SwapUpdatedMessage(SwapRequestUpdate update) => Update = update;

    public SwapRequestUpdate Update { get; }
}

public sealed class ScheduleUpdatedMessage
{
    public ScheduleUpdatedMessage(ScheduleUpdate update) => Update = update;

    public ScheduleUpdate Update { get; }
}
