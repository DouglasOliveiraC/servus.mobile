namespace Servus.Mobile.Services.SwapHistory;

// Must keep Servus.Domain.Enums.SwapCause's order: the API sends the numeric value.
public enum SwapCauseDto
{
    PeerSwap = 0,
    AdminSwap = 1
}

public record SwapHistoryEntryDto(
    Guid Id,
    Guid RosterId,
    DateOnly Date,
    Guid FromMemberId,
    Guid ToMemberId,
    SwapCauseDto Cause,
    Guid? SwapRequestId,
    DateTime CreatedAt);
