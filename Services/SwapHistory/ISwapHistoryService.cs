namespace Servus.Mobile.Services.SwapHistory;

public interface ISwapHistoryService
{
    Task<IReadOnlyList<SwapHistoryEntryDto>> GetBySubrosterIdAsync(Guid subrosterId, CancellationToken cancellationToken = default);
}
