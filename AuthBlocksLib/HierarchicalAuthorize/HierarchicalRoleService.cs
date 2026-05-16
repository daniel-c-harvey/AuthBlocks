using AuthBlocksData.Services;
using AuthBlocksModels.Models;
using Microsoft.Extensions.Logging;

namespace AuthBlocksLib.HierarchicalAuthorize;

public class HierarchicalRoleService : IHierarchicalRoleService
{
    private readonly IRoleService _roleService;
    private readonly ILogger<HierarchicalRoleService> _logger;
    private readonly HierarchicalRoleCache _cache;

    public HierarchicalRoleService(IRoleService roleService, HierarchicalRoleCache cache, ILogger<HierarchicalRoleService> logger)
    {
        _roleService = roleService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasRoleOrInheritsAsync(IList<string> userRoles, string requiredRole)
    {
        if (userRoles.Contains(requiredRole))
        {
            return true;
        }

        if (userRoles.Count == 0)
        {
            return false;
        }

        foreach (var userRole in userRoles)
        {
            if (await InheritsFromRoleAsync(userRole, requiredRole))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> InheritsFromRoleAsync(string userRoleName, string targetRoleName)
    {
        var cacheKey = $"{userRoleName}:{targetRoleName}";

        if (_cache.TryGet(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        try
        {
            if (_cache.IsExpired())
            {
                _cache.Invalidate();
                _logger.LogDebug("Hierarchical role cache refreshed");
            }

            var rolesResult = await _roleService.Get();

            if (rolesResult is { Success: false } or { Value: null })
            {
                _logger.LogDebug("Roles could not be loaded");
                return false;
            }
            var roles = rolesResult.Value!;
            var rolesList = roles.ToList();

            var userRole = rolesList.FirstOrDefault(r => r.Name?.Equals(userRoleName, StringComparison.OrdinalIgnoreCase) == true);
            if (userRole == null)
            {
                _logger.LogDebug("User role '{UserRole}' not found in role hierarchy", userRoleName);
                return false;
            }

            var result = HasChildRole(userRole, targetRoleName, rolesList);

            _cache.Set(cacheKey, result);

            _logger.LogDebug("Role inheritance check: {UserRole} inherits from {TargetRole}: {Result}",
                userRoleName, targetRoleName, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role inheritance from {UserRole} to {TargetRole}",
                userRoleName, targetRoleName);
            return false;
        }
    }

    private static bool HasChildRole(RoleModel userRole, string targetRoleName, List<RoleModel> roles)
    {
        var directChildren = roles.Where(r => r.ParentRole?.Id == userRole.Id).ToList();

        foreach (var child in directChildren)
        {
            if (child.Name?.Equals(targetRoleName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            if (HasChildRole(child, targetRoleName, roles))
            {
                return true;
            }
        }

        return false;
    }
}
