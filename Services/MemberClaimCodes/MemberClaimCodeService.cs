using System.Net.Http.Json;
using Servus.Mobile.Services.Http;

namespace Servus.Mobile.Services.MemberClaimCodes;

public class MemberClaimCodeService : IMemberClaimCodeService
{
    private readonly HttpClient _httpClient;

    public MemberClaimCodeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ClaimMemberResultDto> RedeemAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/member-claim-codes/redeem", new { Code = code }, cancellationToken);
        await response.EnsureSuccessWithProblemAsync(cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClaimMemberResultDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<GeneratedClaimCodeDto> GenerateMemberClaimCodeAsync(Guid organizationId, Guid memberId, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/organization/{organizationId}/members/{memberId}/claim-codes", new { ExpiresAt = expiresAt }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeneratedClaimCodeDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<GeneratedClaimCodeDto> GenerateOrganizationJoinCodeAsync(Guid organizationId, bool isSingleUse, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/organization/{organizationId}/join-codes", new { IsSingleUse = isSingleUse, ExpiresAt = expiresAt }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeneratedClaimCodeDto>(cancellationToken: cancellationToken);
        return result!;
    }
}
