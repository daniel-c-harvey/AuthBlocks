namespace AuthBlocksLib.HierarchicalAuthorize;

/// <summary>
/// Singleton cache for hierarchical role inheritance lookups.
/// Extracted from <see cref="HierarchicalRoleService"/> so the cache survives request boundaries
/// even though <see cref="HierarchicalRoleService"/> itself must remain Scoped (it depends on the
/// Scoped <c>IRoleService</c>).
/// </summary>
public sealed class HierarchicalRoleCache
{
    private readonly Dictionary<string, bool> _entries = new();
    private readonly object _lock = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _expiry = TimeSpan.FromMinutes(5);

    public bool TryGet(string key, out bool value)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(key, out value);
        }
    }

    public void Set(string key, bool value)
    {
        lock (_lock)
        {
            _entries[key] = value;
            _lastRefresh = DateTime.UtcNow;
        }
    }

    public bool IsExpired()
    {
        lock (_lock)
        {
            return DateTime.UtcNow - _lastRefresh > _expiry;
        }
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _entries.Clear();
            _lastRefresh = DateTime.MinValue;
        }
    }
}
