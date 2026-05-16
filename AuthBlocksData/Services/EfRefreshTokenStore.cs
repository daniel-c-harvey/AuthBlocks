using AuthBlocksData.Data;
using AuthBlocksData.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthBlocksData.Services;

public class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly AuthDbContext _context;

    public EfRefreshTokenStore(AuthDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(string tokenHash, long userId, DateTimeOffset expiresAt)
    {
        var entity = new RefreshTokenEntity
        {
            TokenHash = tokenHash,
            UserId = userId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Set<RefreshTokenEntity>().Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ValidateAsync(string tokenHash, long userId)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.Set<RefreshTokenEntity>()
            .AsNoTracking()
            .AnyAsync(t =>
                t.TokenHash == tokenHash &&
                t.UserId == userId &&
                t.RevokedAt == null &&
                t.ExpiresAt > now);
    }

    public async Task RevokeAsync(string tokenHash)
    {
        await _context.Set<RefreshTokenEntity>()
            .Where(t => t.TokenHash == tokenHash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow));
    }

    public async Task<int> DeleteExpiredAsync()
    {
        // Load-then-remove (rather than ExecuteDeleteAsync) keeps this provider-agnostic — the
        // InMemory provider rejects bulk-delete translation. Runs once a day; the set of expired
        // rows is small, so the round-trip cost is irrelevant.
        var now = DateTimeOffset.UtcNow;
        var expired = await _context.Set<RefreshTokenEntity>()
            .Where(t => t.ExpiresAt <= now)
            .ToListAsync();

        if (expired.Count == 0)
        {
            return 0;
        }

        _context.Set<RefreshTokenEntity>().RemoveRange(expired);
        await _context.SaveChangesAsync();
        return expired.Count;
    }
}
