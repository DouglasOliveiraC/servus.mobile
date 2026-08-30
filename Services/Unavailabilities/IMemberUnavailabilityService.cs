namespace Servus.Mobile.Services.Unavailabilities;

public interface IMemberUnavailabilityService
{
    Task<Guid> CreateAsync(Guid memberId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemberUnavailabilityDto>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
