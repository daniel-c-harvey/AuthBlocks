using AuthBlocksModels.Entities.Identity;
using System.Security.Claims;

namespace AuthBlocksAPI.Services;

public interface IJwtService
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
    Task<string> GenerateRefreshTokenAsync();
    ClaimsPrincipal? ValidateToken(string token);
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, long userId);
    Task SaveRefreshTokenAsync(string refreshToken, long userId);
    Task RevokeRefreshTokenAsync(string refreshToken);
} 