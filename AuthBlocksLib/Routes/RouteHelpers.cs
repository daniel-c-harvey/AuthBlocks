using System.Linq.Expressions;
using System.Security.Claims;
using Data.Shared.Managers;
using Microsoft.AspNetCore.Http;
using Models.Shared.Common;
using Models.Shared.Entities;
using Models.Shared.Models;
using NetBlocks.Models;

namespace AuthBlocksLib.Routes;

/// <summary>
/// Shared helpers used by the AuthBlocks Minimal API route groups.
/// These mirror the behaviour of <c>API.Shared.Controllers.ModelController{TEntity,TModel,TManager}</c>
/// so the route surface preserves the exact pattern callers already use.
/// </summary>
internal static class RouteHelpers
{
    public static long GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
    }

    public static IList<string> GetCurrentUserRoles(ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

    public static Expression<Func<TEntity, object>>? Sort<TEntity>(
        Dictionary<string, Expression<Func<TEntity, object>>> sortExpressions, string? sort)
    {
        if (string.IsNullOrEmpty(sort)) return null;
        return sortExpressions.TryGetValue(sort, out var expr) ? expr : null;
    }

    public static async Task<IResult> GetById<TEntity, TModel>(
        IManager<TEntity, TModel> manager, long id)
        where TEntity : class, IEntity
        where TModel : class, IModel
    {
        var getResult = await manager.GetById(id);
        var result = ApiResult<TModel>.From(getResult);
        var dto = new ApiResultDto<TModel>(result);

        if (!result.Success) return Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
        return result.Value == null ? Results.NotFound(dto) : Results.Ok(dto);
    }

    public static async Task<IResult> GetAll<TEntity, TModel>(IManager<TEntity, TModel> manager)
        where TEntity : class, IEntity
        where TModel : class, IModel
    {
        var queryResult = await manager.Get();
        var result = ApiResult<IEnumerable<TModel>>.From(queryResult);
        var dto = new ApiResultDto<IEnumerable<TModel>>(result);
        return result.Success ? Results.Ok(dto) : Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
    }

    public static async Task<IResult> GetPage<TEntity, TModel>(
        IManager<TEntity, TModel> manager,
        PagedQuery query,
        Func<Expression<Func<TEntity, bool>>, PagingParameters<TEntity>, Task<ResultContainer<PagedResult<TModel>>>>? customGetPage,
        Func<string?, Expression<Func<TEntity, bool>>> buildSearchPredicate,
        Dictionary<string, Expression<Func<TEntity, object>>> sortExpressions)
        where TEntity : class, IEntity
        where TModel : class, IModel
    {
        var paging = new PagingParameters<TEntity>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            OrderBy = Sort(sortExpressions, query.Sort),
            IsDescending = query.Desc
        };

        var predicate = buildSearchPredicate(query.Search);
        var pageResult = customGetPage is not null
            ? await customGetPage(predicate, paging)
            : await manager.GetPage(predicate, paging);

        var result = ApiResult<PagedResult<TModel>>.From(pageResult);
        var dto = new ApiResultDto<PagedResult<TModel>>(result);
        return result.Success ? Results.Ok(dto) : Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
    }

    public static async Task<IResult> GetCount<TEntity, TModel>(
        IManager<TEntity, TModel> manager,
        PagedQuery query,
        Func<string?, Expression<Func<TEntity, bool>>> buildSearchPredicate,
        Dictionary<string, Expression<Func<TEntity, object>>> sortExpressions)
        where TEntity : class, IEntity
        where TModel : class, IModel
    {
        var paging = new PagingParameters<TEntity>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            OrderBy = Sort(sortExpressions, query.Sort),
            IsDescending = query.Desc
        };
        var predicate = buildSearchPredicate(query.Search);
        var countResult = await manager.GetPageCount(predicate, paging);

        var result = ApiResult<ItemCount>.From(countResult);
        result.Value = new ItemCount { Count = countResult.Value };
        var dto = new ApiResultDto<ItemCount>(result);
        return result.Success ? Results.Ok(dto) : Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
    }

    public static async Task<IResult> Post<TEntity, TModel>(IManager<TEntity, TModel> manager, TModel model)
        where TEntity : class, IEntity
        where TModel : class, IModel
    {
        var existsResult = await manager.Exists(model);
        if (existsResult is not { Success: true })
        {
            return Results.Json(ApiResult<TModel>.From(existsResult), statusCode: StatusCodes.Status500InternalServerError);
        }

        if (existsResult.Value)
        {
            var updateResult = await manager.Update(model);
            var result = ApiResult<TModel>.From(updateResult);
            result.Value = model;
            var dto = new ApiResultDto<TModel>(result);
            return result.Success
                ? Results.Accepted($"?id={model.Id}", dto)
                : Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
        }
        else
        {
            var addResult = await manager.Add(model);
            var result = ApiResult<TModel>.From(addResult);
            result.Value = model;
            var dto = new ApiResultDto<TModel>(result);
            return result.Success
                ? Results.Created($"?id={model.Id}", dto)
                : Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    public static async Task<IResult> Delete<TEntity, TModel>(IManager<TEntity, TModel> manager, long id)
        where TEntity : class, IEntity
        where TModel : class, IModel
    {
        var result = await manager.Delete(id);
        var dto = new ApiResultDto(ApiResult.From(result));
        return result.Success
            ? Results.Ok(dto)
            : Results.Json(dto, statusCode: StatusCodes.Status500InternalServerError);
    }
}
