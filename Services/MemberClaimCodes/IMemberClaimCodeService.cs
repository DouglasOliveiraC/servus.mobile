namespace Servus.Mobile.Services.MemberClaimCodes;

public interface IMemberClaimCodeService
{
    Task<ClaimMemberResultDto> RedeemAsync(string code, CancellationToken cancellationToken = default);

    Task<GeneratedClaimCodeDto> GenerateMemberClaimCodeAsync(Guid rosterId, Guid memberId, DateTime? expiresAt, CancellationToken cancellationToken = default);

    Task<GeneratedClaimCodeDto> GenerateRosterJoinCodeAsync(Guid rosterId, bool isSingleUse, DateTime? expiresAt, CancellationToken cancellationToken = default);
}
