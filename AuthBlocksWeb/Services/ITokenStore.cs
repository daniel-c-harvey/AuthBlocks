namespace AuthBlocksWeb.Services;

/// <summary>
/// Pure token storage: reads and writes JWT access/refresh tokens in browser
/// localStorage. No knowledge of auth APIs, refresh logic, or the Blazor
/// authentication cascade — those concerns live in <see cref="IAuthSession"/>.
/// </summary>
public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task ClearTokensAsync();
    Task<bool> IsTokenValidAsync();
}
