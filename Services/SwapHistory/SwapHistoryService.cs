using System.Net.Http.Json;

namespace Servus.Mobile.Services.SwapHistory;

public class SwapHistoryService : ISwapHistoryService
{
    private readonly HttpClient _httpClient;

    public SwapHistoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SwapHistoryEntryDto>> GetBySubrosterIdAsync(Guid subrosterId, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<SwapHistoryEntryDto>>($"api/swap-history?subrosterId={subrosterId}", cancellationToken);
        return result ?? [];
    }
}
