using AuthBlocksModels.Entities.Identity;
using AuthBlocksModels.Models;
using Data.Shared.Managers;
using NetBlocks.Models;

namespace AuthBlocksData.Services;

public interface IRoleService : IManager<ApplicationRole, RoleModel>
{
    Task<ResultContainer<RoleModel>> FindByNameAsync(string roleName);
}