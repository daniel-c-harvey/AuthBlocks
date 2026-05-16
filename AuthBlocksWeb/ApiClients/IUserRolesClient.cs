using AuthBlocksModels.ApiModels;
using NetBlocks.Models;

namespace AuthBlocksWeb.ApiClients;

public interface IUserRolesClient
{
    Task<ApiResult<List<RoleInfo>>> GetRolesForUser(long userId);
    Task<ApiResult> AddUserToRole(long userId, string roleName);
    Task<ApiResult> RemoveUserFromRole(long userId, string roleName);
}
