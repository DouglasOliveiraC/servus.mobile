namespace Servus.Mobile.Services.SwapHistory;

public enum SwapCauseDto
{
    Manual = 0,
    Approved = 1,
    Automatic = 2
}

public record SwapHistoryEntryDto(
    Guid Id,
    Guid SubrosterId,
    DateOnly Date,
    Guid FromMemberId,
    Guid ToMemberId,
    SwapCauseDto Cause,
    Guid? SwapRequestId,
    DateTime CreatedAt);
