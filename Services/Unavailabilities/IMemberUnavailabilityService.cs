namespace Servus.Mobile.Services.Unavailabilities;

public interface IMemberUnavailabilityService
{
    /// <summary>
    /// O resultado diz se o período já vale ou se ficou aguardando aprovação de um
    /// administrador — a tela precisa dizer coisas diferentes ao usuário em cada caso.
    /// </summary>
    Task<CreateUnavailabilityResult> CreateAsync(Guid memberId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemberUnavailabilityDto>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
