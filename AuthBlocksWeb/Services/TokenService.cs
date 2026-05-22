using AuthBlocksModels.ApiModels;
using AuthBlocksWeb.ApiClients;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;

namespace AuthBlocksWeb.Services;

public class TokenService : ITokenService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IAuthApiClient _authApiClient;
    // ISessionExpiredAction (the JwtAuthenticationStateProvider) takes ITokenService,
    // so direct injection would close a construction-time cycle. Lazy<T> breaks the
    // cycle: both objects construct independently and the dependency only resolves
    // the first time a session-expiry path actually fires.
    private readonly Lazy<ISessionExpiredAction> _sessionExpiredAction;
    private const string AccessTokenKey = "authblocks_access_token";
    private const string RefreshTokenKey = "authblocks_refresh_token";

    public TokenService(
        IJSRuntime jsRuntime,
        IAuthApiClient authApiClient,
        Lazy<ISessionExpiredAction> sessionExpiredAction)
    {
        _jsRuntime = jsRuntime;
        _authApiClient = authApiClient;
        _sessionExpiredAction = sessionExpiredAction;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, accessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
        }
        catch
        {
            // Silently fail if localStorage is not available
        }
    }

    public async Task ClearTokensAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        }
        catch
        {
            // Silently fail if localStorage is not available
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtToken = jwtHandler.ReadJwtToken(token);

            // Check if token is expired (with 1 minute buffer)
            return jwtToken.ValidTo > DateTime.UtcNow.AddMinutes(1);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetValidTokenAsync()
    {
        if (await IsTokenValidAsync())
        {
            return await GetAccessTokenAsync();
        }

        if (await TryRefreshInternalAsync())
        {
            return await GetAccessTokenAsync();
        }

        // Both checks failed — clear tokens and notify the cascade.
        await ClearTokensAsync();
        await _sessionExpiredAction.Value.HandleAsync();
        return null;
    }

    public async Task<string?> ForceRefreshAsync()
    {
        if (await TryRefreshInternalAsync())
        {
            return await GetAccessTokenAsync();
        }

        // Refresh failed — clear tokens and notify the cascade.
        await ClearTokensAsync();
        await _sessionExpiredAction.Value.HandleAsync();
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
            var accessToken = await GetAccessTokenAsync();
            var refreshToken = await GetRefreshTokenAsync();
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
                await SetTokensAsync(response.Value.AccessToken, response.Value.RefreshToken);
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
