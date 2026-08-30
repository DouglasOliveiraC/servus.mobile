using System.Net;
using System.Net.Http.Json;

namespace Servus.Mobile.Services.Members;

public class MemberService : IMemberService
{
    private readonly HttpClient _httpClient;

    public MemberService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MemberDto?> GetMyMemberAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/member/me", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MemberDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<MemberDto>> SearchMembersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/member" : $"api/member?search={Uri.EscapeDataString(search)}";
        var result = await _httpClient.GetFromJsonAsync<List<MemberDto>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<MemberDto> CreateMemberAsync(string name, string? email = null, string? phone = null, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/member", new { Name = name, Email = email, Phone = phone }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreateMemberResult>(cancellationToken: cancellationToken);
        return new MemberDto(created!.Id, name, email, phone, null, DateTime.UtcNow);
    }

    public async Task UpdateMemberAsync(Guid id, string name, string? email, string? phone, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/member/{id}", new { Name = name, Email = email, Phone = phone }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteMemberAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/member/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MemberMembershipDto>> GetMembershipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<MemberMembershipDto>>($"api/member/{id}/memberships", cancellationToken);
        return result ?? [];
    }

    private record CreateMemberResult(Guid Id);
}
