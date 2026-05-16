namespace AuthBlocksData.Data.Entities;

/// <summary>
/// Persisted refresh-token record. We store a SHA-256 hash of the token rather than the
/// plaintext so that database compromise does not yield usable session credentials.
/// </summary>
public class RefreshTokenEntity
{
    public long Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
