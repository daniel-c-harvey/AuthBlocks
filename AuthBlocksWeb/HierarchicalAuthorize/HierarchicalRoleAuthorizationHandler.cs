using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AuthBlocksWeb.HierarchicalAuthorize;

public class HierarchicalRolesAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>
{
    private readonly IHierarchicalRoleService _hierarchicalRoleService;
    private readonly ILogger<HierarchicalRolesAuthorizationHandler> _logger;

    public HierarchicalRolesAuthorizationHandler(
        IHierarchicalRoleService hierarchicalRoleService,
        ILogger<HierarchicalRolesAuthorizationHandler> logger)
    {
        _hierarchicalRoleService = hierarchicalRoleService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolesAuthorizationRequirement requirement)
    {
        var user = context.User;
        
        _logger.LogDebug("HierarchicalRolesAuthorizationHandler: Checking authorization for user {UserName} with required roles {RequiredRoles}", 
            user.Identity?.Name, string.Join(", ", requirement.AllowedRoles));

        if (!(user.Identity?.IsAuthenticated ?? false))
        {
            _logger.LogDebug("HierarchicalRolesAuthorizationHandler: User not authenticated");
            return;
        }

        // If no roles are specified, just being authenticated is enough
        if (!requirement.AllowedRoles.Any())
        {
            _logger.LogDebug("HierarchicalRolesAuthorizationHandler: No roles required, user is authenticated");
            context.Succeed(requirement);
            return;
        }

        // Get user's roles from JWT claims
        var userRoles = user.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        _logger.LogDebug("HierarchicalRolesAuthorizationHandler: User roles from claims: {UserRoles}", 
            string.Join(", ", userRoles));

        // Debug: Also log to console for immediate visibility
        Console.WriteLine($"HierarchicalRolesAuthorizationHandler: User '{user.Identity?.Name}' has roles: {string.Join(", ", userRoles)}");
        Console.WriteLine($"HierarchicalRolesAuthorizationHandler: Required roles: {string.Join(", ", requirement.AllowedRoles)}");

        // Check if user has any of the required roles or inherits from them
        foreach (var requiredRole in requirement.AllowedRoles)
        {
            var hasRole = await _hierarchicalRoleService.HasRoleOrInheritsAsync(userRoles, requiredRole);
            _logger.LogDebug("HierarchicalRolesAuthorizationHandler: User has role {RequiredRole}: {HasRole}", 
                requiredRole, hasRole);
            
            Console.WriteLine($"HierarchicalRolesAuthorizationHandler: User has role {requiredRole}: {hasRole}");
            
            if (hasRole)
            {
                _logger.LogDebug("HierarchicalRolesAuthorizationHandler: Authorization succeeded");
                Console.WriteLine("HierarchicalRolesAuthorizationHandler: Authorization succeeded");
                context.Succeed(requirement);
                return;
            }
        }

        _logger.LogWarning("HierarchicalRolesAuthorizationHandler: Authorization failed for user {UserName}", 
            user.Identity?.Name);
        Console.WriteLine($"HierarchicalRolesAuthorizationHandler: Authorization failed for user {user.Identity?.Name}");
    }
} 