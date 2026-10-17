namespace Servus.Mobile.Services.MemberClaimCodes;

public record GeneratedClaimCodeDto(string Code, DateTime? ExpiresAt);

public record ClaimMemberResultDto(Guid MemberId, Guid OrganizationId);
