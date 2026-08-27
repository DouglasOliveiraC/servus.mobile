namespace Servus.Mobile.Services.Rosters;

public interface IRosterService
{
    Task<IReadOnlyList<RosterDto>> GetMyRostersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubrosterDto>> GetSubrostersAsync(Guid rosterId, CancellationToken cancellationToken = default);

    Task<SubrosterScheduleDto> GetScheduleAsync(Guid subrosterId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<Guid?> GetMyMemberIdAsync(CancellationToken cancellationToken = default);
}
