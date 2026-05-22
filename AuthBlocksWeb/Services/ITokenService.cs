namespace AuthBlocksWeb.Services;

public interface ITokenService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task ClearTokensAsync();
    Task<bool> IsTokenValidAsync();

    /// <summary>
    /// Returns a valid access token, refreshing silently if the current one is
    /// expired. If no valid token can be obtained (refresh token also expired or
    /// missing), clears stored tokens, notifies the auth cascade via
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
