using System.Linq.Expressions;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using AuthBlocksModels.SystemDefinitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Models.Shared.Common;
using NetBlocks.Models;

namespace AuthBlocksLib.Routes;

internal static class RolesRoutes
{
    private static readonly Dictionary<string, Expression<Func<ApplicationRole, object>>> SortExpressions = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(ApplicationRole.Id)] = e => e.Id,
        [nameof(RoleModel.Name)] = e => e.Name ?? string.Empty,
        [nameof(RoleModel.NormalizedName)] = e => e.NormalizedName ?? string.Empty,
        ["CreatedAt"] = e => e.CreatedAt,
        ["UpdatedAt"] = e => e.UpdatedAt,
    };

    private static Expression<Func<ApplicationRole, bool>> BuildSearch(string? search)
    {
        if (string.IsNullOrEmpty(search))
            return e => true;

        return e => e.Name!.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.NormalizedName!.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    public static void Map(IEndpointRouteBuilder root)
    {
        // Class-level [Authorize] on RolesController → group-level authenticated requirement.
        var group = root.MapGroup("api/roles").RequireAuthorization();

        // GET /api/roles (paged) — UserAdmin only
        group.MapGet("", async ([AsParameters] PagedQuery query, IRoleService roleService) =>
                await RouteHelpers.GetPage<ApplicationRole, RoleModel>(roleService, query, null, BuildSearch, SortExpressions))
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        // GET /api/roles/all
        group.MapGet("all", async (IRoleService roleService) =>
            await RouteHelpers.GetAll<ApplicationRole, RoleModel>(roleService));

        // GET /api/roles/count
        group.MapGet("count", async ([AsParameters] PagedQuery query, IRoleService roleService) =>
            await RouteHelpers.GetCount<ApplicationRole, RoleModel>(roleService, query, BuildSearch, SortExpressions));

        // GET /api/roles/{id} — UserAdmin only
        group.MapGet("{id:long}", async (long id, IRoleService roleService) =>
                await RouteHelpers.GetById<ApplicationRole, RoleModel>(roleService, id))
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        // POST /api/roles — UserAdmin only
        group.MapPost("", async ([FromBody] RoleModel model, IRoleService roleService) =>
                await RouteHelpers.Post<ApplicationRole, RoleModel>(roleService, model))
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        // DELETE /api/roles/{id} — UserAdmin only; rejects system roles
        group.MapDelete("{id:long}", async (long id, IRoleService roleService) =>
            {
                var rolesResult = await roleService.Get();
                if (rolesResult is { Success: false } or { Value: null })
                {
                    return Results.Json(new ApiResultDto(ApiResult.From(rolesResult)),
                        statusCode: StatusCodes.Status500InternalServerError);
                }
                var roles = rolesResult.Value!;

                if (SystemRole.GetAll()
                    .Join(roles, sr => sr.Name, r => r.Name, (_, r) => r.Id)
                    .Contains(id))
                {
                    var resultFailure = ApiResult.CreateFailResult("Cannot delete system role")
                        .Fail("Admin role cannot be deleted");
                    return Results.BadRequest(new ApiResultDto(resultFailure));
                }

                return await RouteHelpers.Delete<ApplicationRole, RoleModel>(roleService, id);
            })
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));
    }
}
