using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthBlocksData.Services;
using AuthBlocksLib.Models;
using AuthBlocksLib.Services;
using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MsOptions = Microsoft.Extensions.Options;
using NetBlocks.Models;
using NSubstitute;
using Xunit;

namespace AuthBlocksLib.Tests;

/// <summary>
/// Verifies the refresh-token flow in <see cref="JwtService"/>:
///  - <see cref="JwtService.ValidateExpiredToken"/> accepts a legitimately-expired token
///    while still rejecting tampered/wrong-issuer tokens.
///  - <see cref="JwtService.ValidateRefreshTokenAsync"/> enforces revocation and expiry.
/// </summary>
public class JwtServiceRefreshTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private const string Secret = "super-secret-key-that-is-at-least-32-bytes!!";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const long UserId = 42L;

    private static JwtSettings DefaultSettings() => new()
    {
        Secret = Secret,
        Issuer = Issuer,
        Audience = Audience,
        ExpiryMinutes = 60,
        RefreshTokenExpiryDays = 7
    };

    /// <summary>Creates a <see cref="JwtService"/> wired to an in-memory refresh-token store.</summary>
    private static (JwtService service, InMemoryRefreshTokenStore store) BuildService(JwtSettings? settings = null)
    {
        var opts = MsOptions.Options.Create(settings ?? DefaultSettings());
        var store = new InMemoryRefreshTokenStore();
        var userRoleService = Substitute.For<IUserRoleService>();
        return (new JwtService(opts, userRoleService, store), store);
    }

    /// <summary>
    /// Issues a JWT that is already expired (nbf = now - 2 min, exp = now - 1 min).
    /// Signed with the canonical secret/issuer/audience unless overrides are supplied.
    /// </summary>
    private static string IssueExpiredAccessToken(
        string? secret = null,
        string? issuer = null,
        string? audience = null)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret ?? Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: now.AddMinutes(-2),
            expires: now.AddMinutes(-1),        // already expired
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string RandomRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    // ── 1. Expired access token + valid refresh token → success ──────────────

    [Fact]
    public async Task ValidateExpiredToken_AcceptsExpiredToken_WhenSignatureValid()
    {
        var (service, store) = BuildService();
        var expiredJwt = IssueExpiredAccessToken();
        var refreshToken = RandomRefreshToken();
        await service.SaveRefreshTokenAsync(refreshToken, UserId);

        // ValidateToken (lifetime-enforcing) must return null for an expired token.
        Assert.Null(service.ValidateToken(expiredJwt));

        // ValidateExpiredToken must return a principal.
        var principal = service.ValidateExpiredToken(expiredJwt);
        Assert.NotNull(principal);

        // Refresh-token must still be valid.
        var refreshValid = await service.ValidateRefreshTokenAsync(refreshToken, UserId);
        Assert.True(refreshValid);
    }

    // ── 2. Expired access token + revoked refresh token → fail ───────────────

    [Fact]
    public async Task ValidateRefreshToken_Fails_WhenRefreshTokenRevoked()
    {
        var (service, store) = BuildService();
        var expiredJwt = IssueExpiredAccessToken();
        var refreshToken = RandomRefreshToken();
        await service.SaveRefreshTokenAsync(refreshToken, UserId);
        await service.RevokeRefreshTokenAsync(refreshToken);

        // Access token structure is valid (only expired).
        Assert.NotNull(service.ValidateExpiredToken(expiredJwt));

        // But refresh token is revoked → should fail.
        var refreshValid = await service.ValidateRefreshTokenAsync(refreshToken, UserId);
        Assert.False(refreshValid);
    }

    // ── 3. Expired access token + expired refresh token → fail ───────────────

    [Fact]
    public async Task ValidateRefreshToken_Fails_WhenRefreshTokenExpired()
    {
        var settings = DefaultSettings();
        settings.RefreshTokenExpiryDays = 0; // expires immediately (in the past)
        var (service, store) = BuildService(settings);

        var expiredJwt = IssueExpiredAccessToken();
        var refreshToken = RandomRefreshToken();

        // Manually insert a refresh token that is already past its expiry.
        var tokenHash = HashToken(refreshToken);
        await store.SaveAsync(tokenHash, UserId, DateTimeOffset.UtcNow.AddSeconds(-1));

        Assert.NotNull(service.ValidateExpiredToken(expiredJwt));

        var refreshValid = await service.ValidateRefreshTokenAsync(refreshToken, UserId);
        Assert.False(refreshValid);
    }

    // ── 4. Tampered-signature (expired) access token + valid refresh token → fail ──

    [Fact]
    public async Task ValidateExpiredToken_Fails_WhenSignatureTampered()
    {
        var (service, store) = BuildService();
        var refreshToken = RandomRefreshToken();
        await service.SaveRefreshTokenAsync(refreshToken, UserId);

        var expiredJwt = IssueExpiredAccessToken();

        // Tamper: replace the signature segment with random bytes.
        var parts = expiredJwt.Split('.');
        parts[2] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tamperedJwt = string.Join('.', parts);

        var principal = service.ValidateExpiredToken(tamperedJwt);
        Assert.Null(principal);
    }

    // ── 5. Wrong-issuer / wrong-audience access token + valid refresh token → fail ──

    [Fact]
    public void ValidateExpiredToken_Fails_WhenIssuerWrong()
    {
        var (service, _) = BuildService();
        var wrongIssuerJwt = IssueExpiredAccessToken(issuer: "evil-issuer");

        Assert.Null(service.ValidateExpiredToken(wrongIssuerJwt));
    }

    [Fact]
    public void ValidateExpiredToken_Fails_WhenAudienceWrong()
    {
        var (service, _) = BuildService();
        var wrongAudienceJwt = IssueExpiredAccessToken(audience: "wrong-audience");

        Assert.Null(service.ValidateExpiredToken(wrongAudienceJwt));
    }

    // ── 7. ValidateExpiredToken does not mutate the shared validation parameters ──

    [Fact]
    public async Task ValidateExpiredToken_DoesNotMutate_SharedValidationParameters()
    {
        // Arrange: a single JwtService instance with a real expired token.
        var (service, store) = BuildService();
        var expiredJwt = IssueExpiredAccessToken();
        var refreshToken = RandomRefreshToken();
        await service.SaveRefreshTokenAsync(refreshToken, UserId);

        // Act: ValidateExpiredToken succeeds (lifetime check disabled for this call).
        var principal = service.ValidateExpiredToken(expiredJwt);
        Assert.NotNull(principal);

        // Assert: the shared _tokenValidationParameters are unchanged — the global
        // ValidateToken path must still reject the same expired token.
        Assert.Null(service.ValidateToken(expiredJwt));
    }

    // ── 6. Old refresh token rejected after successful rotation ──────────────

    [Fact]
    public async Task RefreshTokenRotation_OldToken_RejectedAfterRotation()
    {
        var (service, store) = BuildService();
        var expiredJwt = IssueExpiredAccessToken();
        var originalRefresh = RandomRefreshToken();
        await service.SaveRefreshTokenAsync(originalRefresh, UserId);

        // Simulate what RefreshToken endpoint does: revoke old, issue new.
        Assert.True(await service.ValidateRefreshTokenAsync(originalRefresh, UserId));
        await service.RevokeRefreshTokenAsync(originalRefresh);
        var newRefresh = RandomRefreshToken();
        await service.SaveRefreshTokenAsync(newRefresh, UserId);

        // Old token must now be invalid.
        Assert.False(await service.ValidateRefreshTokenAsync(originalRefresh, UserId));
        // New token must be valid.
        Assert.True(await service.ValidateRefreshTokenAsync(newRefresh, UserId));
    }

    // ── helper: mirror JwtService's internal hash so test 3 can insert directly ──

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

// ── In-memory IRefreshTokenStore for tests ───────────────────────────────────

/// <summary>
/// Thread-safe in-memory <see cref="IRefreshTokenStore"/> for unit tests.
/// Mirrors the validation logic of <see cref="EfRefreshTokenStore"/> without EF.
/// </summary>
internal sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly record struct Entry(long UserId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);

    private readonly Dictionary<string, Entry> _store = new();

    public Task SaveAsync(string tokenHash, long userId, DateTimeOffset expiresAt)
    {
        _store[tokenHash] = new Entry(userId, expiresAt, null);
        return Task.CompletedTask;
    }

    public Task<bool> ValidateAsync(string tokenHash, long userId)
    {
        var now = DateTimeOffset.UtcNow;
        var valid = _store.TryGetValue(tokenHash, out var e)
                    && e.UserId == userId
                    && e.RevokedAt is null
                    && e.ExpiresAt > now;
        return Task.FromResult(valid);
    }

    public Task RevokeAsync(string tokenHash)
    {
        if (_store.TryGetValue(tokenHash, out var e) && e.RevokedAt is null)
            _store[tokenHash] = e with { RevokedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = _store.Where(kv => kv.Value.ExpiresAt <= now).Select(kv => kv.Key).ToList();
        foreach (var key in expired) _store.Remove(key);
        return Task.FromResult(expired.Count);
    }
}

// ── Regression guard: AuthRoutes.RefreshToken endpoint ───────────────────────

/// <summary>
/// Guards the bug fix that changed <c>AuthRoutes.RefreshToken</c> from calling
/// <c>jwtService.ValidateToken</c> (lifetime-enforcing) to
/// <c>jwtService.ValidateExpiredToken</c> (lifetime-skipping).
///
/// If someone reverts line 294 of AuthRoutes.cs back to <c>ValidateToken</c>,
/// the real <see cref="JwtService"/> will return null for an expired access token
/// and the handler will return 400 — causing this test to fail.
/// </summary>
public class AuthRouteRefreshTokenTests
{
    private const string Secret = "super-secret-key-that-is-at-least-32-bytes!!";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const long UserId = 42L;

    private static JwtSettings DefaultSettings() => new()
    {
        Secret = Secret,
        Issuer = Issuer,
        Audience = Audience,
        ExpiryMinutes = 60,
        RefreshTokenExpiryDays = 7
    };

    /// <summary>
    /// Issues a JWT that is already expired, signed with the canonical key.
    /// </summary>
    private static string IssueExpiredAccessToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: now.AddMinutes(-2),
            expires: now.AddMinutes(-1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string RandomRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    // AuthRoutes and AuthLogger are internal to AuthBlocksLib — resolve via the assembly
    // of a public type from that library rather than referencing the internal names directly.
    private static readonly Assembly AuthBlocksLibAssembly =
        typeof(JwtService).Assembly;

    private static readonly Type AuthRoutesType =
        AuthBlocksLibAssembly.GetType("AuthBlocksLib.Routes.AuthRoutes")
        ?? throw new InvalidOperationException(
            "AuthBlocksLib.Routes.AuthRoutes not found — class was renamed or moved.");

    // Reflective handle for the private static handler — resolved once per class.
    private static readonly MethodInfo RefreshTokenMethod =
        AuthRoutesType.GetMethod(
            "RefreshToken",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException(
            "AuthRoutes.RefreshToken not found — method was renamed or removed.");

    // Resolve the internal AuthLogger type so we can substitute ILogger<AuthLogger>.
    private static readonly Type AuthLoggerType =
        AuthRoutesType.GetNestedType("AuthLogger", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException(
            "AuthRoutes.AuthLogger not found — nested type was renamed or removed.");

    private static object MakeNullLogger()
    {
        // NullLogger<T> implements ILogger<T> without Castle DynamicProxy, which cannot
        // proxy closed generics over internal types in strong-named assemblies.
        // We resolve the closed generic type at runtime to avoid a compile-time reference
        // to the internal AuthLogger type.
        var nullLoggerType = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>)
            .MakeGenericType(AuthLoggerType);
        return Activator.CreateInstance(nullLoggerType)!;
    }

    // ── Regression guard ─────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_EndpointAcceptsExpiredAccessToken_AndReturnsNewTokenPair()
    {
        // Arrange ─────────────────────────────────────────────────────────────
        var settings = DefaultSettings();
        var jwtSettings = settings;

        // Real JwtService: ValidateToken rejects expired tokens; ValidateExpiredToken accepts them.
        var userRoleService = Substitute.For<IUserRoleService>();
        userRoleService
            .GetByUser(Arg.Any<UserModel>())
            .Returns(ResultContainer<IEnumerable<RoleModel>>.CreatePassResult(
                Array.Empty<RoleModel>()));

        var store = new InMemoryRefreshTokenStore();
        var opts = MsOptions.Options.Create(settings);
        var jwtService = new JwtService(opts, userRoleService, store);

        var expiredAccessToken = IssueExpiredAccessToken();
        var originalRefreshToken = RandomRefreshToken();
        await jwtService.SaveRefreshTokenAsync(originalRefreshToken, UserId);

        var user = new UserModel { Id = UserId, UserName = "testuser", Email = "test@example.com" };
        var userService = Substitute.For<IUserService>();
        userService
            .GetById(UserId)
            .Returns(ResultContainer<UserModel>.CreatePassResult(user));

        var request = new RefreshTokenRequest
        {
            AccessToken = expiredAccessToken,
            RefreshToken = originalRefreshToken
        };

        var logger = MakeNullLogger();

        // Act — invoke the private static handler via reflection ──────────────
        var task = (Task<IResult>)RefreshTokenMethod.Invoke(
            null,
            [request, userService, userRoleService, jwtService, jwtSettings, logger])!;
        var result = await task;

        // Assert ──────────────────────────────────────────────────────────────
        // Must be HTTP 200 OK.
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);

        // The response must carry a new (rotated) access+refresh token pair,
        // both different from the inputs. ApiResultDto<T>.From() converts the DTO
        // back to an ApiResult<T> with Success/Value accessible.
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        dynamic dto = valueResult.Value!;
        dynamic apiResult = dto.From();
        Assert.True((bool)apiResult.Success);
        string newAccessToken = (string)apiResult.Value.AccessToken;
        string newRefreshToken = (string)apiResult.Value.RefreshToken;
        Assert.NotEqual(expiredAccessToken, newAccessToken);
        Assert.NotEqual(originalRefreshToken, newRefreshToken);
    }
}
