using System.Linq.Expressions;
using System.Security.Claims;
using AuthBlocksData.Services;
using AuthBlocksLib.HierarchicalAuthorize;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using AuthBlocksModels.SystemDefinitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Models.Common;
using NetBlocks.Models;

namespace AuthBlocksLib.Routes;

internal static class UsersRoutes
{
    private static readonly Dictionary<string, Expression<Func<ApplicationUser, object>>> SortExpressions = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(ApplicationUser.Id)] = e => e.Id,
        [nameof(UserModel.UserName)] = e => e.UserName ?? string.Empty,
        [nameof(UserModel.Email)] = e => e.Email ?? string.Empty,
        [nameof(UserModel.EmailConfirmed)] = e => e.EmailConfirmed,
        [nameof(UserModel.PhoneNumber)] = e => e.PhoneNumber ?? string.Empty,
        [nameof(UserModel.PhoneNumberConfirmed)] = e => e.PhoneNumberConfirmed,
        [nameof(UserModel.TwoFactorEnabled)] = e => e.TwoFactorEnabled,
        [nameof(UserModel.LockoutEnabled)] = e => e.LockoutEnabled,
        [nameof(UserModel.AccessFailedCount)] = e => e.AccessFailedCount,
        ["CreatedAt"] = e => e.CreatedAt,
        ["UpdatedAt"] = e => e.UpdatedAt,
    };

    private static Expression<Func<ApplicationUser, bool>> BuildSearch(string? search)
    {
        if (string.IsNullOrEmpty(search))
            return e => true;

        return e => e.UserName!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.Email!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.NormalizedUserName!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.NormalizedEmail!.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    public static void Map(IEndpointRouteBuilder root)
    {
        // Class-level [Authorize] on UsersController → group-level authenticated requirement.
        var group = root.MapGroup("api/users").RequireAuthorization();

        // GET /api/users (paged)
        group.MapGet("", async (
                [AsParameters] PagedQuery query,
                ClaimsPrincipal principal,
                IUserService userService) =>
            {
                var currentUserId = RouteHelpers.GetCurrentUserId(principal);
                return await RouteHelpers.GetPage<ApplicationUser, UserModel>(
                    userService,
                    query,
                    (predicate, parameters) => userService.GetPage(currentUserId, predicate, parameters),
                    BuildSearch,
                    SortExpressions);
            })
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        // GET /api/users/all
        group.MapGet("all", async (IUserService userService) =>
            await RouteHelpers.GetAll<ApplicationUser, UserModel>(userService));

        // GET /api/users/count
        group.MapGet("count", async ([AsParameters] PagedQuery query, IUserService userService) =>
            await RouteHelpers.GetCount<ApplicationUser, UserModel>(userService, query, BuildSearch, SortExpressions));

        // GET /api/users/{id}
        group.MapGet("{id:long}", async (
            long id,
            ClaimsPrincipal principal,
            IUserService userService) =>
        {
            var currentUserId = RouteHelpers.GetCurrentUserId(principal);
            if (currentUserId != id && !principal.IsInRole(SystemRole.UserAdmin))
            {
                return Results.Forbid();
            }
            return await RouteHelpers.GetById<ApplicationUser, UserModel>(userService, id);
        });

        // POST /api/users — bare [HierarchicalRoleAuthorize] (no role arg) is equivalent to [Authorize].
        group.MapPost("", async (
            [FromBody] UserModel model,
            ClaimsPrincipal principal,
            IUserService userService,
            IHierarchicalRoleService authRoleService) =>
        {
            var currentUserId = RouteHelpers.GetCurrentUserId(principal);
            if (currentUserId != model.Id &&
                !await authRoleService.HasRoleOrInheritsAsync(RouteHelpers.GetCurrentUserRoles(principal), SystemRoleConstants.UserAdmin))
            {
                return Results.Forbid();
            }
            return await RouteHelpers.Post<ApplicationUser, UserModel>(userService, model);
        });

        // DELETE /api/users/{id}
        group.MapDelete("{id:long}", async (
                long id,
                ClaimsPrincipal principal,
                IUserService userService) =>
            {
                // Prevent user from deleting themselves
                var currentUserId = RouteHelpers.GetCurrentUserId(principal);
                if (currentUserId == id)
                {
                    var resultFailure = ApiResult.CreateFailResult("Cannot delete your own account")
                        .Fail("Self-deletion not allowed");
                    return Results.BadRequest(new ApiResultDto(resultFailure));
                }
                return await RouteHelpers.Delete<ApplicationUser, UserModel>(userService, id);
            })
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));
    }
}
