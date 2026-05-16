using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public interface IUserRoleService : IManager<ApplicationUserRole, UserRoleModel>
{
    Task<Result> AddUserToRoleAsync(UserModel user, string roleName);
    Task<Result> RemoveUserFromRoleAsync(UserModel user, string roleName);
    Task<ResultContainer<IEnumerable<RoleModel>>> GetByUser(UserModel user);
}