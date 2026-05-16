using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;

namespace AuthBlocksWeb.Services;

public class TokenService : ITokenService
{
    private readonly IJSRuntime _jsRuntime;
    private const string AccessTokenKey = "authblocks_access_token";
    private const string RefreshTokenKey = "authblocks_refresh_token";

    public TokenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
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
} 