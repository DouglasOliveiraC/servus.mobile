using System.Net.Http.Json;

namespace Servus.Mobile.Services.Users;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<UserDto>($"api/users/{id}", cancellationToken);
        return result!;
    }

    public async Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/display-name", new { DisplayName = displayName }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/password", new { CurrentPassword = currentPassword, NewPassword = newPassword }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RequestEmailChangeAsync(Guid id, string newEmail, string currentPassword, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}/email", new { NewEmail = newEmail, CurrentPassword = currentPassword }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
