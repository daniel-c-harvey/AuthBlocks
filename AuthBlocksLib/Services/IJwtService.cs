using System.Security.Claims;
using AuthBlocksModels.Models;

namespace AuthBlocksLib.Services;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(UserModel user);
    Task<string> GenerateRefreshTokenAsync();
    ClaimsPrincipal? ValidateToken(string token);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, long userId);
    Task SaveRefreshTokenAsync(string refreshToken, long userId);
    Task RevokeRefreshTokenAsync(string refreshToken);
}
