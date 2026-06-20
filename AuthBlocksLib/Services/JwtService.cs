using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthBlocksData.Services;
using AuthBlocksLib.Models;
using AuthBlocksModels.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthBlocksLib.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IUserRoleService _userRoleService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtService(
        IOptions<JwtSettings> jwtSettings,
        IUserRoleService userRoleService,
        IRefreshTokenStore refreshTokenStore)
    {
        _jwtSettings = jwtSettings.Value;
        _userRoleService = userRoleService;
        _refreshTokenStore = refreshTokenStore;

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

    public async Task<string> GenerateTokenAsync(UserModel user)
    {
        var roleResult = await _userRoleService.GetByUser(user);
        if (roleResult is { Success: false } or { Value: null })
            throw new Exception("Could not determine roles for user");

        var roles = roleResult.Value!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Name!)));

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
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc cref="IJwtService.ValidateExpiredToken"/>
    public ClaimsPrincipal? ValidateExpiredToken(string token)
    {
        try
        {
            var noLifetimeParams = _tokenValidationParameters.Clone();
            noLifetimeParams.ValidateLifetime = false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, noLifetimeParams, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken, long userId)
    {
        return _refreshTokenStore.ValidateAsync(HashToken(refreshToken), userId);
    }

    public Task SaveRefreshTokenAsync(string refreshToken, long userId)
    {
        var expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        return _refreshTokenStore.SaveAsync(HashToken(refreshToken), userId, expires);
    }

    public Task RevokeRefreshTokenAsync(string refreshToken)
    {
        return _refreshTokenStore.RevokeAsync(HashToken(refreshToken));
    }

    // SHA-256 is enough here: tokens are 32 bytes of CSPRNG output, so the hash is collision- and
    // preimage-resistant in practice and indexable as a fixed-width string. No salt needed because
    // the input is already high-entropy and unique per issuance.
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
