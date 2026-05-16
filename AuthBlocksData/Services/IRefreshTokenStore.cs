namespace AuthBlocksData.Services;

/// <summary>
/// Persistence boundary for refresh tokens. Callers hash plaintext tokens before invocation —
/// this store never sees the raw token. Lives next to its EF implementation because
/// <see cref="EfRefreshTokenStore"/> depends on AuthDbContext and AuthBlocksData cannot reference
/// the consumer assembly (AuthBlocksLib) without a circular project reference.
/// </summary>
public interface IRefreshTokenStore
{
    Task SaveAsync(string tokenHash, long userId, DateTimeOffset expiresAt);

    /// <summary>
    /// Returns true when a record exists for <paramref name="tokenHash"/> bound to
    /// <paramref name="userId"/> that has not been revoked and has not expired.
    /// </summary>
    Task<bool> ValidateAsync(string tokenHash, long userId);

    /// <summary>Marks the token as revoked. No-op if the hash is unknown.</summary>
    Task RevokeAsync(string tokenHash);

    /// <summary>
    /// Removes rows whose <c>ExpiresAt</c> is in the past. Returns the number of rows deleted —
    /// useful for sweep logging.
    /// </summary>
    Task<int> DeleteExpiredAsync();
}
