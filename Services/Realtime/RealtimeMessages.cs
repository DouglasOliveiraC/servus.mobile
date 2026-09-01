namespace Servus.Mobile.Services.Realtime;

/// <summary>Payload shapes mirror IRealtimeNotifier's notification records on the backend
/// (Servus.Application.Interfaces.RealtimeInterfaces) — kept as separate types here since the
/// mobile app has no reference to Servus.Application.</summary>
public record SwapRequestUpdate(Guid SwapRequestId, Guid RosterId, string Status);

public record SchedulePublishedUpdate(Guid RosterId, int AssignmentsPublished);

public sealed class SwapUpdatedMessage
{
    public SwapUpdatedMessage(SwapRequestUpdate update) => Update = update;

    public SwapRequestUpdate Update { get; }
}

public sealed class SchedulePublishedMessage
{
    public SchedulePublishedMessage(SchedulePublishedUpdate update) => Update = update;

    public SchedulePublishedUpdate Update { get; }
}
