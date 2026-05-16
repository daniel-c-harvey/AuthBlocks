using AuthBlocksModels.ApiModels;
using AuthBlocksModels.Models;
using AuthBlocksWeb.ApiClients;
using Models.Shared.Common;
using NetBlocks.Models;

namespace AuthBlocksWeb.Components.Pages.UserAdmin.Permissions;

/// <summary>
/// Backing logic for the Permissions admin page: user lookup, role retrieval,
/// and role assign/revoke. Pure orchestration over the existing API clients —
/// no UI state is held here so the razor component owns rendering concerns alone.
/// </summary>
public class PermissionsViewModel
{
    private readonly UsersClient _usersClient;
    private readonly IUserRolesClient _userRolesClient;
    private readonly IAuthApiClient _authApiClient;

    public PermissionsViewModel(
        UsersClient usersClient,
        IUserRolesClient userRolesClient,
        IAuthApiClient authApiClient)
    {
        _usersClient = usersClient;
        _userRolesClient = userRolesClient;
        _authApiClient = authApiClient;
    }

    public async Task<IEnumerable<UserModel>> SearchUsers(string? search, CancellationToken ct = default)
    {
        var query = new PagedQuery
        {
            Page = 1,
            PageSize = 20,
            Search = search,
            Sort = nameof(UserModel.UserName)
        };

        var result = await _usersClient.GetByPage(query);

        if (result is { Success: true, Value: { } page })
        {
            return page.Items ?? Enumerable.Empty<UserModel>();
        }
        return Enumerable.Empty<UserModel>();
    }

    public Task<ApiResult<List<RoleInfo>>> GetRolesForUser(long userId)
        => _userRolesClient.GetRolesForUser(userId);

    public Task<ApiResult<List<RoleInfo>>> GetAllRoles()
        => _authApiClient.GetRolesAsync();

    public Task<ApiResult> AssignRole(long userId, string roleName)
        => _userRolesClient.AddUserToRole(userId, roleName);

    public Task<ApiResult> RevokeRole(long userId, string roleName)
        => _userRolesClient.RemoveUserFromRole(userId, roleName);
}
