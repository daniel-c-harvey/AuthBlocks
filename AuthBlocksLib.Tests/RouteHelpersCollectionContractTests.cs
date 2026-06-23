using System.Linq.Expressions;
using System.Reflection;
using AuthBlocksLib.Services;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Data.Managers;
using Microsoft.AspNetCore.Http;
using Models.Common;
using NetBlocks.Models;
using NSubstitute;
using Xunit;

namespace AuthBlocksLib.Tests;

/// <summary>
/// Guards the non-null-collection contract of <c>RouteHelpers.GetPage</c> and
/// <c>RouteHelpers.GetAll</c> — the generic helpers backing every AuthBlocks model
/// route (mirroring <c>ModelController&lt;TEntity,TModel,TManager&gt;</c>).
///
/// The consuming Web grid (<c>ModelView</c>) runs LINQ operators over the returned
/// collection without guarding it for null (the null-conditional sits on the page,
/// not on <c>Items</c>). A successful response carrying a null collection therefore
/// throws <c>ArgumentNullException (Parameter 'source')</c> in the client before any
/// row renders. These tests pin the server-side guarantee that a successful paged or
/// get-all response never delivers a null collection — empty is an empty collection,
/// not null.
///
/// <c>RouteHelpers</c> is internal; like <see cref="AuthRouteRefreshTokenTests"/> we
/// reach it via reflection rather than widening visibility.
/// </summary>
public class RouteHelpersCollectionContractTests
{
    private static readonly Type RouteHelpersType =
        typeof(JwtService).Assembly.GetType("AuthBlocksLib.Routes.RouteHelpers")
        ?? throw new InvalidOperationException(
            "AuthBlocksLib.Routes.RouteHelpers not found — class was renamed or moved.");

    private static readonly MethodInfo GetPageMethod =
        RouteHelpersType.GetMethod("GetPage", BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(ApplicationUser), typeof(UserModel))
        ?? throw new InvalidOperationException("RouteHelpers.GetPage not found.");

    private static readonly MethodInfo GetAllMethod =
        RouteHelpersType.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(ApplicationUser), typeof(UserModel))
        ?? throw new InvalidOperationException("RouteHelpers.GetAll not found.");

    private static readonly Expression<Func<ApplicationUser, bool>> AllRows = _ => true;
    private static readonly Func<string?, Expression<Func<ApplicationUser, bool>>> BuildSearch = _ => AllRows;
    private static readonly Dictionary<string, Expression<Func<ApplicationUser, object>>> NoSort = new();

    private static IManager<ApplicationUser, UserModel> Manager() =>
        Substitute.For<IManager<ApplicationUser, UserModel>>();

    private static async Task<ApiResult<PagedResult<UserModel>>> InvokeGetPage(
        IManager<ApplicationUser, UserModel> manager)
    {
        var task = (Task<IResult>)GetPageMethod.Invoke(
            null,
            [manager, new PagedQuery(), null, BuildSearch, NoSort])!;
        var result = await task;
        return UnwrapApiResult<PagedResult<UserModel>>(result);
    }

    private static async Task<ApiResult<IEnumerable<UserModel>>> InvokeGetAll(
        IManager<ApplicationUser, UserModel> manager)
    {
        var task = (Task<IResult>)GetAllMethod.Invoke(null, [manager])!;
        var result = await task;
        return UnwrapApiResult<IEnumerable<UserModel>>(result);
    }

    /// <summary>Pulls the serialized DTO out of the IResult and rebuilds the ApiResult.</summary>
    private static ApiResult<T> UnwrapApiResult<T>(IResult result)
    {
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        var dto = (ApiResultDto<T>)valueResult.Value!;
        return dto.From();
    }

    // ── GetPage ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPage_EmptyPage_ReturnsSuccessWithNonNullEmptyItems()
    {
        var manager = Manager();
        // The manager succeeds but the page carries a null Items collection — the
        // exact shape that, unguarded, throws in the client grid.
        var emptyPage = new PagedResult<UserModel> { Items = null! };
        manager.GetPage(Arg.Any<Expression<Func<ApplicationUser, bool>>>(), Arg.Any<PagingParameters<ApplicationUser>>())
            .Returns(ResultContainer<PagedResult<UserModel>>.CreatePassResult(emptyPage));

        var apiResult = await InvokeGetPage(manager);

        Assert.True(apiResult.Success);
        Assert.NotNull(apiResult.Value);
        Assert.NotNull(apiResult.Value!.Items);
        Assert.Empty(apiResult.Value.Items);
    }

    [Fact]
    public async Task GetPage_PopulatedPage_PreservesItems()
    {
        var manager = Manager();
        var users = new List<UserModel>
        {
            new() { Id = 1, UserName = "admin", Email = "admin@example.com" },
            new() { Id = 2, UserName = "editor", Email = "editor@example.com" },
        };
        var page = new PagedResult<UserModel> { Items = users, TotalCount = 2, Page = 1, PageSize = 10 };
        manager.GetPage(Arg.Any<Expression<Func<ApplicationUser, bool>>>(), Arg.Any<PagingParameters<ApplicationUser>>())
            .Returns(ResultContainer<PagedResult<UserModel>>.CreatePassResult(page));

        var apiResult = await InvokeGetPage(manager);

        Assert.True(apiResult.Success);
        Assert.NotNull(apiResult.Value);
        Assert.Equal(2, apiResult.Value!.Items.Count());
        Assert.Equal([1, 2], apiResult.Value.Items.Select(u => u.Id));
    }

    [Fact]
    public async Task GetPage_AlreadyEmptyItems_RemainNonNullAndEmpty()
    {
        var manager = Manager();
        // The normal server path already yields a non-null empty list; the guard
        // must be a no-op here, not replace a valid empty collection.
        var page = new PagedResult<UserModel> { Items = new List<UserModel>() };
        manager.GetPage(Arg.Any<Expression<Func<ApplicationUser, bool>>>(), Arg.Any<PagingParameters<ApplicationUser>>())
            .Returns(ResultContainer<PagedResult<UserModel>>.CreatePassResult(page));

        var apiResult = await InvokeGetPage(manager);

        Assert.True(apiResult.Success);
        Assert.NotNull(apiResult.Value!.Items);
        Assert.Empty(apiResult.Value.Items);
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NullEnumerable_ReturnsSuccessWithNonNullEmptyCollection()
    {
        var manager = Manager();
        manager.Get(Arg.Any<Expression<Func<ApplicationUser, bool>>?>())
            .Returns(ResultContainer<IEnumerable<UserModel>>.CreatePassResult(null));

        var apiResult = await InvokeGetAll(manager);

        Assert.True(apiResult.Success);
        Assert.NotNull(apiResult.Value);
        Assert.Empty(apiResult.Value!);
    }

    [Fact]
    public async Task GetAll_PopulatedEnumerable_PreservesItems()
    {
        var manager = Manager();
        var users = new List<UserModel> { new() { Id = 7, UserName = "u7", Email = "u7@example.com" } };
        manager.Get(Arg.Any<Expression<Func<ApplicationUser, bool>>?>())
            .Returns(ResultContainer<IEnumerable<UserModel>>.CreatePassResult(users));

        var apiResult = await InvokeGetAll(manager);

        Assert.True(apiResult.Success);
        Assert.NotNull(apiResult.Value);
        Assert.Equal(7, Assert.Single(apiResult.Value!).Id);
    }
}
