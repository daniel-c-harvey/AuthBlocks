using System.Linq.Expressions;
using AuthBlocksData.Services;
using AuthBlocksModels.ApiModels;
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

internal static class UserRolesRoutes
{
    private static readonly Dictionary<string, Expression<Func<ApplicationUserRole, object>>> SortExpressions = new(StringComparer.OrdinalIgnoreCase)
    {
        // ApplicationUserRole has composite key (UserId, RoleId); base "Id" sort is via IEntity.Id.
        // Mirroring the base ModelController defaults only — UserRolesController adds no custom sorts.
    };

    private static Expression<Func<ApplicationUserRole, bool>> BuildSearch(string? _) => e => true;

    public static void Map(IEndpointRouteBuilder root)
    {
        // Class-level [Authorize] on UserRolesController → group-level authenticated requirement.
        var group = root.MapGroup("api/userroles").RequireAuthorization();

        // Base ModelController CRUD surface
        group.MapGet("", async ([AsParameters] PagedQuery query, IUserRoleService userRoleService) =>
            await RouteHelpers.GetPage<ApplicationUserRole, UserRoleModel>(userRoleService, query, null, BuildSearch, SortExpressions));

        group.MapGet("all", async (IUserRoleService userRoleService) =>
            await RouteHelpers.GetAll<ApplicationUserRole, UserRoleModel>(userRoleService));

        group.MapGet("count", async ([AsParameters] PagedQuery query, IUserRoleService userRoleService) =>
            await RouteHelpers.GetCount<ApplicationUserRole, UserRoleModel>(userRoleService, query, BuildSearch, SortExpressions));

        group.MapGet("{id:long}", async (long id, IUserRoleService userRoleService) =>
            await RouteHelpers.GetById<ApplicationUserRole, UserRoleModel>(userRoleService, id));

        group.MapPost("", async ([FromBody] UserRoleModel model, IUserRoleService userRoleService) =>
            await RouteHelpers.Post<ApplicationUserRole, UserRoleModel>(userRoleService, model));

        group.MapDelete("{id:long}", async (long id, IUserRoleService userRoleService) =>
            await RouteHelpers.Delete<ApplicationUserRole, UserRoleModel>(userRoleService, id));

        // Custom: POST /api/userroles/user/{userId:long}
        // Original controller used [FromQuery] long userId despite the route segment;
        // mirror the exact binding source to preserve behaviour.
        //
        // RequireRole is intentional — the registered HierarchicalRolesAuthorizationHandler intercepts this
        // so Admin (or any role that inherits UserAdmin) can manage roles.
        group.MapGet("user/{userId:long}", GetRolesForUser)
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        group.MapPost("user/{userId:long}", AddUserToRole)
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));

        group.MapDelete("user/{userId:long}", RemoveUserFromRole)
            .RequireAuthorization(policy => policy.RequireRole(SystemRoleConstants.UserAdmin));
    }

    private static async Task<IResult> GetRolesForUser(
        [FromRoute] long userId,
        IUserService userService,
        IUserRoleService userRoleService)
    {
        try
        {
            var userResult = await userService.GetById(userId);
            switch (userResult)
            {
                case { Success: false }:
                    return Results.Json(new ApiResultDto<List<RoleInfo>>(ApiResult<List<RoleInfo>>.From(userResult)),
                        statusCode: StatusCodes.Status500InternalServerError);
                case { Value: null }:
                    var notFound = ApiResult<List<RoleInfo>>.CreateFailResult("User not found").Fail("User does not exist");
                    return Results.NotFound(new ApiResultDto<List<RoleInfo>>(notFound));
            }

            var rolesResult = await userRoleService.GetByUser(userResult.Value!);
            if (rolesResult is { Success: false } or { Value: null })
            {
                return Results.Json(new ApiResultDto<List<RoleInfo>>(ApiResult<List<RoleInfo>>.From(rolesResult)),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var roleInfos = rolesResult.Value!.Select(r => new RoleInfo
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                NormalizedName = r.NormalizedName,
                ParentRoleId = r.ParentRole?.Id,
                ParentRoleName = r.ParentRole?.Name,
                Created = r.CreatedAt,
                Modified = r.UpdatedAt
            }).ToList();

            var success = ApiResult<List<RoleInfo>>.CreatePassResult(roleInfos)
                .Inform("Roles for user retrieved successfully");
            return Results.Ok(new ApiResultDto<List<RoleInfo>>(success));
        }
        catch
        {
            var resultError = ApiResult<List<RoleInfo>>.CreateFailResult("An error occurred while retrieving roles for user")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto<List<RoleInfo>>(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> AddUserToRole(
        [FromRoute] long userId,
        [FromBody] UserRoleRequest request,
        IUserService userService,
        IUserRoleService userRoleService)
    {
        try
        {
            var userResult = await userService.GetById(userId);
            switch (userResult)
            {
                case { Success: false }:
                    return Results.Json(new ApiResultDto(ApiResult.From(userResult)),
                        statusCode: StatusCodes.Status500InternalServerError);
                case { Value: null }:
                    var notFound = ApiResult.CreateFailResult("User not found").Fail("User does not exist");
                    return Results.NotFound(new ApiResultDto(notFound));
            }

            await userRoleService.AddUserToRoleAsync(userResult.Value!, request.RoleName);

            var resultSuccess = ApiResult.CreatePassResult()
                .Inform($"User added to role '{request.RoleName}' successfully");
            return Results.Ok(new ApiResultDto(resultSuccess));
        }
        catch
        {
            var resultError = ApiResult.CreateFailResult("An error occurred while adding user to role")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> RemoveUserFromRole(
        [FromRoute] long userId,
        [FromBody] UserRoleRequest request,
        IUserService userService,
        IUserRoleService userRoleService)
    {
        try
        {
            var userResult = await userService.GetById(userId);
            switch (userResult)
            {
                case { Success: false }:
                    return Results.Json(new ApiResultDto(ApiResult.From(userResult)),
                        statusCode: StatusCodes.Status500InternalServerError);
                case { Value: null }:
                    var notFound = ApiResult.CreateFailResult("User not found").Fail("User does not exist");
                    return Results.NotFound(new ApiResultDto(notFound));
            }

            await userRoleService.RemoveUserFromRoleAsync(userResult.Value!, request.RoleName);

            var resultSuccess = ApiResult.CreatePassResult()
                .Inform($"User removed from role '{request.RoleName}' successfully");
            return Results.Ok(new ApiResultDto(resultSuccess));
        }
        catch
        {
            var resultError = ApiResult.CreateFailResult("An error occurred while removing user from role")
                .Fail("Internal server error");
            return Results.Json(new ApiResultDto(resultError), statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
