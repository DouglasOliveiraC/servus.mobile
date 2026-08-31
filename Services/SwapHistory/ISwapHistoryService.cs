namespace Servus.Mobile.Services.SwapHistory;

public interface ISwapHistoryService
{
    Task<IReadOnlyList<SwapHistoryEntryDto>> GetByRosterIdAsync(Guid rosterId, CancellationToken cancellationToken = default);
}
