using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;

namespace AuthBlocksData.Data.Repositories;

public interface IUserRoleRepository : IRepository<ApplicationUserRole>
{
    Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName);
    Task<IList<ApplicationRole>> GetRolesAsync(ApplicationUser user);
    Task<IList<ApplicationRole>> GetRolesAsync(long userId);
    Task<bool> IsInRoleAsync(ApplicationUser user, string roleName);
    Task AddToRoleAsync(ApplicationUser user, string roleName);
    Task RemoveFromRoleAsync(ApplicationUser user, string roleName);
}