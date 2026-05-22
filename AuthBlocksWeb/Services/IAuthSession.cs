namespace AuthBlocksWeb.Services;

/// <summary>
/// Session lifecycle: obtains a usable access token (refreshing transparently
/// when needed) and notifies the Blazor auth cascade via
/// <see cref="ISessionExpiredAction"/> when the session cannot be recovered.
/// </summary>
public interface IAuthSession
{
    /// <summary>
    /// Returns a valid access token, refreshing silently if the current one is
    /// expired. If no valid token can be obtained (refresh token also expired
    /// or missing), clears stored tokens, notifies the auth cascade via
    /// <see cref="ISessionExpiredAction"/>, and returns <c>null</c>.
    /// </summary>
    Task<string?> GetValidTokenAsync();

    /// <summary>
    /// Forces a token refresh regardless of the current token's validity (for
    /// use after a 401 response). Same fallback behaviour as
    /// <see cref="GetValidTokenAsync"/> when refresh fails.
    /// </summary>
    Task<string?> ForceRefreshAsync();
}
