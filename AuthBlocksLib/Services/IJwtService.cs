using System.Security.Claims;
using AuthBlocksModels.Models;

namespace AuthBlocksLib.Services;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(UserModel user);
    Task<string> GenerateRefreshTokenAsync();
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Validates the access token's signature, issuer, audience, and signing key, but does NOT
    /// enforce lifetime. Use only on the refresh path where the token is expected to be expired;
    /// refresh-token validity gates re-issuance instead.
    /// </summary>
    ClaimsPrincipal? ValidateExpiredToken(string token);

    Task<bool> ValidateRefreshTokenAsync(string refreshToken, long userId);
    Task SaveRefreshTokenAsync(string refreshToken, long userId);
    Task RevokeRefreshTokenAsync(string refreshToken);
}
