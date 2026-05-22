using AuthBlocksModels.ApiModels;
using AuthBlocksWeb.ApiClients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;

namespace AuthBlocksWeb.Services;

public class TokenService : ITokenService
{
    private readonly IJSRuntime _jsRuntime;
    // IAuthApiClient depends (transitively) on ITokenService, so direct injection
    // would close a construction-time cycle. We resolve it lazily from the scope
    // at call time, by which point both objects are fully constructed.
    // ISessionExpiredAction is resolved the same way for symmetry and to keep
    // TokenService usable in contexts where the Blazor cascade isn't wired up
    // (e.g. tests, background services).
    private readonly IServiceProvider _serviceProvider;
    private const string AccessTokenKey = "authblocks_access_token";
    private const string RefreshTokenKey = "authblocks_refresh_token";

    public TokenService(IJSRuntime jsRuntime, IServiceProvider serviceProvider)
    {
        _jsRuntime = jsRuntime;
        _serviceProvider = serviceProvider;
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
        await _serviceProvider.GetRequiredService<ISessionExpiredAction>().HandleAsync();
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
        await _serviceProvider.GetRequiredService<ISessionExpiredAction>().HandleAsync();
        return null;
    }

    /// <summary>
    /// Calls the refresh endpoint with the currently stored access + refresh
    /// tokens. On success <see cref="AuthApiClient"/> writes the new pair to
    /// the token store before returning, so no duplicate storage is needed
    /// here. Any failure (missing tokens, server rejection, exception) is
    /// reported as <c>false</c>.
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

            var authApiClient = _serviceProvider.GetRequiredService<IAuthApiClient>();
            var response = await authApiClient.RefreshTokenAsync(new RefreshTokenRequest
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            });
            return response is { Success: true, Value: not null };
        }
        catch
        {
            return false;
        }
    }
}
