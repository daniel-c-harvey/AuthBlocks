using AuthBlocksAPI.Models;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksData.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace AuthBlocksAPI.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserService _userService;
    private readonly TokenValidationParameters _tokenValidationParameters;
    
    // Simple in-memory refresh token store (in production, use database)
    private static readonly Dictionary<string, (long UserId, DateTime Expires)> _refreshTokens = new();

    public JwtService(IOptions<JwtSettings> jwtSettings, UserService userService)
    {
        _jwtSettings = jwtSettings.Value;
        _userService = userService;
        
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var userRoles = await _userService.GetActiveUserRolesAsync(user);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Task.FromResult(Convert.ToBase64String(randomNumber));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, long userId)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var tokenData))
        {
            return Task.FromResult(tokenData.UserId == userId && tokenData.Expires > DateTime.UtcNow);
        }
        return Task.FromResult(false);
    }

    public Task SaveRefreshTokenAsync(string refreshToken, long userId)
    {
        var expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        _refreshTokens[refreshToken] = (userId, expires);
        
        // Clean up expired tokens
        var expiredTokens = _refreshTokens.Where(t => t.Value.Expires <= DateTime.UtcNow).ToList();
        foreach (var expiredToken in expiredTokens)
        {
            _refreshTokens.Remove(expiredToken.Key);
        }
        
        return Task.CompletedTask;
    }

    public Task RevokeRefreshTokenAsync(string refreshToken)
    {
        _refreshTokens.Remove(refreshToken);
        return Task.CompletedTask;
    }
} 