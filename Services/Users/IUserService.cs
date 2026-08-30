namespace Servus.Mobile.Services.Users;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task RequestEmailChangeAsync(Guid id, string newEmail, string currentPassword, CancellationToken cancellationToken = default);

    Task DeleteAccountAsync(Guid id, CancellationToken cancellationToken = default);
}
