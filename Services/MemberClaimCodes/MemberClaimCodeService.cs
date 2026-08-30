using System.Net.Http.Json;

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
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClaimMemberResultDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<GeneratedClaimCodeDto> GenerateMemberClaimCodeAsync(Guid rosterId, Guid memberId, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/roster/{rosterId}/members/{memberId}/claim-codes", new { ExpiresAt = expiresAt }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeneratedClaimCodeDto>(cancellationToken: cancellationToken);
        return result!;
    }

    public async Task<GeneratedClaimCodeDto> GenerateRosterJoinCodeAsync(Guid rosterId, bool isSingleUse, DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/roster/{rosterId}/join-codes", new { IsSingleUse = isSingleUse, ExpiresAt = expiresAt }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeneratedClaimCodeDto>(cancellationToken: cancellationToken);
        return result!;
    }
}
