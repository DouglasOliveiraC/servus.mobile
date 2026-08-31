using System.Net.Http.Json;

namespace Servus.Mobile.Services.Organizations;

public class OrganizationService : IOrganizationService
{
    private readonly HttpClient _httpClient;

    public OrganizationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<OrganizationDto>> GetMyOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<OrganizationDto>>("api/organization", cancellationToken);
        return result ?? [];
    }

    public async Task<OrganizationDto> GetOrganizationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<OrganizationDto>($"api/organization/{id}", cancellationToken);
        return result!;
    }

    public async Task<Guid> CreateOrganizationAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/organization", new { Name = name }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreatedResult>(cancellationToken: cancellationToken);
        return result!.Id;
    }

    public async Task UpdateOrganizationNameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/organization/{id}", new { Name = name }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteOrganizationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/organization/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private record CreatedResult(Guid Id);
}
