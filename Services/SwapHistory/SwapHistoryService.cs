using System.Net.Http.Json;

namespace Servus.Mobile.Services.SwapHistory;

public class SwapHistoryService : ISwapHistoryService
{
    private readonly HttpClient _httpClient;

    public SwapHistoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SwapHistoryEntryDto>> GetByRosterIdAsync(Guid rosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SwapHistoryEntryDto>>($"api/swap-history?rosterId={rosterId}", cancellationToken);
        return result ?? [];
    }
}
