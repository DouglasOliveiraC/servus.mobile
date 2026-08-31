namespace Servus.Mobile.Services.MemberClaimCodes;

public interface IMemberClaimCodeService
{
    Task<ClaimMemberResultDto> RedeemAsync(string code, CancellationToken cancellationToken = default);

    Task<GeneratedClaimCodeDto> GenerateMemberClaimCodeAsync(Guid organizationId, Guid memberId, DateTime? expiresAt, CancellationToken cancellationToken = default);

    Task<GeneratedClaimCodeDto> GenerateOrganizationJoinCodeAsync(Guid organizationId, bool isSingleUse, DateTime? expiresAt, CancellationToken cancellationToken = default);
}
