using AuthBlocksModels.ApiModels;
using AuthBlocksWeb.ApiClients;

namespace AuthBlocksWeb.Services;

public class AuthSession : IAuthSession
{
    private readonly ITokenStore _tokenStore;
    private readonly IAuthApiClient _authApiClient;
    private readonly ISessionExpiredAction _sessionExpiredAction;

    public AuthSession(
        ITokenStore tokenStore,
        IAuthApiClient authApiClient,
        ISessionExpiredAction sessionExpiredAction)
    {
        _tokenStore = tokenStore;
        _authApiClient = authApiClient;
        _sessionExpiredAction = sessionExpiredAction;
    }

    public async Task<string?> GetValidTokenAsync()
    {
        if (await _tokenStore.IsTokenValidAsync())
        {
            return await _tokenStore.GetAccessTokenAsync();
        }

        if (await TryRefreshInternalAsync())
        {
            return await _tokenStore.GetAccessTokenAsync();
        }

        // Both checks failed — clear tokens and notify the cascade.
        await _tokenStore.ClearTokensAsync();
        await _sessionExpiredAction.HandleAsync();
        return null;
    }

    public async Task<string?> ForceRefreshAsync()
    {
        if (await TryRefreshInternalAsync())
        {
            return await _tokenStore.GetAccessTokenAsync();
        }

        // Refresh failed — clear tokens and notify the cascade.
        await _tokenStore.ClearTokensAsync();
        await _sessionExpiredAction.HandleAsync();
        return null;
    }

    /// <summary>
    /// Calls the refresh endpoint with the currently stored access + refresh
    /// tokens. On success, stores the new pair before returning so callers
    /// can immediately read the fresh access token. Any failure (missing
    /// tokens, server rejection, exception) is reported as <c>false</c>.
    /// </summary>
    private async Task<bool> TryRefreshInternalAsync()
    {
        try
        {
            var accessToken = await _tokenStore.GetAccessTokenAsync();
            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var response = await _authApiClient.RefreshTokenAsync(new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            });

            if (response is { Success: true, Value: not null })
            {
                await _tokenStore.SetTokensAsync(response.Value.AccessToken, response.Value.RefreshToken);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
