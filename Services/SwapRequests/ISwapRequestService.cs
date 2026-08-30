namespace Servus.Mobile.Services.SwapRequests;

public interface ISwapRequestService
{
    Task<Guid> RequestSwapAsync(Guid requestingDutyAssignmentId, Guid targetDutyAssignmentId, Guid requestingMemberId, CancellationToken cancellationToken = default);

    Task<SwapRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SwapRequestDto>> GetPendingAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task RespondAsync(Guid id, Guid respondingMemberId, bool accept, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid id, Guid cancellingMemberId, CancellationToken cancellationToken = default);
}
