using System.Linq.Expressions;
using AuthBlocksData.Data.Repositories;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Data.Managers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Models.Common;
using NSubstitute;
using Xunit;

namespace AuthBlocksLib.Tests;

/// <summary>
/// Pins the contract of <see cref="UserService.GetPage"/>: the Users paged route wires a
/// custom delegate to this method (bypassing the generic <c>RouteHelpers.GetPage</c> guard),
/// so the non-null-collection guarantee has to be enforced here too. The consuming CMS grid
/// runs LINQ over <c>Items</c> without a null guard; a successful response carrying null
/// <c>Items</c> throws <c>ArgumentNullException (Parameter 'source')</c> before any row renders.
///
/// These tests drive the real <c>base.GetPage</c> seam by mocking
/// <see cref="IUserRepository.GetPagedAsync(Expression{Func{ApplicationUser, bool}}, PagingParameters{ApplicationUser})"/>.
/// That seam is what reaches production paths: a populated page, an empty page, and a thrown
/// read (the genuine-failure path). The defensive null-<c>Value</c> / null-<c>Items</c> branches
/// in <c>GetPage</c> are not reachable through the concrete (non-virtual-mockable) <c>base.GetPage</c>,
/// which always returns a non-null <c>PagedResult</c> with non-null <c>Items</c>; they guard the
/// version-skew case and are covered by inspection rather than a forced mock.
/// </summary>
public class UserServiceGetPageContractTests
{
    private const long CurrentUserId = 1;

    private static readonly Expression<Func<ApplicationUser, bool>> AllRows = _ => true;

    private static UserService BuildService(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository)
    {
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null, null, null, null, null, null, null, null);
        var roleRepository = Substitute.For<IRoleRepository>();
        var logger = Substitute.For<ILogger<Manager<ApplicationUser, UserModel,
            IUserRepository, AuthBlocksModels.Converters.UserEntityToModelConverter>>>();

        return new UserService(userManager, userRepository, roleRepository, userRoleRepository, logger);
    }

    private static PagingParameters<ApplicationUser> Paging() =>
        new() { Page = 1, PageSize = 10 };

    [Fact]
    public async Task GetPage_PopulatedItems_ReturnsSuccessAndDecoratesCanDelete()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var userRoleRepository = Substitute.For<IUserRoleRepository>();
        // No roles on anyone → CanDeleteRoleCheck clears, so CanDelete is set by id comparison.
        userRoleRepository.GetRolesAsync(Arg.Any<long>())
            .Returns(new List<ApplicationRole>());

        var entities = new List<ApplicationUser>
        {
            new() { Id = CurrentUserId, UserName = "admin", Email = "admin@example.com" },
            new() { Id = 2, UserName = "editor", Email = "editor@example.com" },
        };
        userRepository.GetPagedAsync(Arg.Any<Expression<Func<ApplicationUser, bool>>>(), Arg.Any<PagingParameters<ApplicationUser>>())
            .Returns(new PagedResult<ApplicationUser> { Items = entities, TotalCount = 2, Page = 1, PageSize = 10 });

        var service = BuildService(userRepository, userRoleRepository);

        var result = await service.GetPage(CurrentUserId, AllRows, Paging());

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value!.Items);
        var items = result.Value.Items.ToList();
        Assert.Equal([CurrentUserId, 2], items.Select(u => u.Id));
        // Decoration ran: the current user cannot delete themselves; the other can be deleted.
        Assert.False(items.Single(u => u.Id == CurrentUserId).CanDelete);
        Assert.True(items.Single(u => u.Id == 2).CanDelete);
    }

    [Fact]
    public async Task GetPage_EmptyItems_ReturnsSuccessWithNonNullEmptyItems()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var userRoleRepository = Substitute.For<IUserRoleRepository>();
        userRoleRepository.GetRolesAsync(Arg.Any<long>()).Returns(new List<ApplicationRole>());

        userRepository.GetPagedAsync(Arg.Any<Expression<Func<ApplicationUser, bool>>>(), Arg.Any<PagingParameters<ApplicationUser>>())
            .Returns(new PagedResult<ApplicationUser> { Items = new List<ApplicationUser>(), TotalCount = 0, Page = 1, PageSize = 10 });

        var service = BuildService(userRepository, userRoleRepository);

        var result = await service.GetPage(CurrentUserId, AllRows, Paging());

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value!.Items);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task GetPage_BaseRead_Fails_ReturnsFailPreservingMessages()
    {
        var userRepository = Substitute.For<IUserRepository>();
        var userRoleRepository = Substitute.For<IUserRoleRepository>();

        // base.GetPage catches the thrown read and surfaces the message on a Fail result.
        userRepository.GetPagedAsync(Arg.Any<Expression<Func<ApplicationUser, bool>>>(), Arg.Any<PagingParameters<ApplicationUser>>())
            .Returns<PagedResult<ApplicationUser>>(_ => throw new InvalidOperationException("db unreachable"));

        var service = BuildService(userRepository, userRoleRepository);

        var result = await service.GetPage(CurrentUserId, AllRows, Paging());

        Assert.False(result.Success);
        Assert.Contains(result.Messages, m => m.Message.Contains("db unreachable"));
        // Decoration must not run against a failed read.
        await userRoleRepository.DidNotReceive().GetRolesAsync(Arg.Any<long>());
    }
}
