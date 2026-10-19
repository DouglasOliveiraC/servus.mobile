using System.Net.Http.Json;

namespace Servus.Mobile.Services.SwapRequests;

public class SwapRequestService : ISwapRequestService
{
    private readonly HttpClient _httpClient;

    public SwapRequestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid> RequestSwapAsync(Guid requestingDutyAssignmentId, Guid targetDutyAssignmentId, Guid requestingMemberId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/swap-request", new
        {
            RequestingDutyAssignmentId = requestingDutyAssignmentId,
            TargetDutyAssignmentId = targetDutyAssignmentId,
            RequestingMemberId = requestingMemberId
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task<SwapRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<SwapRequestDto>($"api/swap-request/{id}", cancellationToken);
        return result!;
    }

    public async Task<IReadOnlyList<SwapRequestDto>> GetPendingAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SwapRequestDto>>($"api/swap-request/pending?memberId={memberId}", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SwapRequestDto>> GetAwaitingMyApprovalAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SwapRequestDto>>("api/swap-request/awaiting-approval", cancellationToken);
        return result ?? [];
    }

    public async Task RespondAsync(Guid id, Guid respondingMemberId, bool accept, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/swap-request/{id}/respond", new
        {
            RespondingMemberId = respondingMemberId,
            Accept = accept
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsync($"api/swap-request/{id}/approve", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelAsync(Guid id, Guid cancellingMemberId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/swap-request/{id}?cancellingMemberId={cancellingMemberId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record CreatedResult(Guid Id);
}
