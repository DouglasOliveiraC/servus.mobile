namespace Servus.Mobile.Services.Auth;

public interface ISessionManager
{
    /// <summary>
    /// Raised once the refresh token is rejected by the API: the stored tokens are already
    /// cleared by then, so the only thing left for the subscriber is sending the user to Login.
    /// </summary>
    event EventHandler? SessionExpired;

    /// <summary>
    /// Returns the stored access token, refreshing it first when it is expired or about to
    /// expire. Null means there is no usable session (or the API could not be reached).
    /// </summary>
    Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="rejectedToken"/> is the token the API just refused: if another caller
    /// already replaced it, that newer token is returned instead of refreshing again — the API
    /// rotates refresh tokens and treats reuse of a rotated one as theft.
    /// </summary>
    Task<string?> RefreshAsync(string? rejectedToken, CancellationToken cancellationToken = default);
}
