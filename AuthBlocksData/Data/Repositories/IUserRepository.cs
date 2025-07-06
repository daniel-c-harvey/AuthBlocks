using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(long id);
    Task<ApplicationUser?> GetByUsernameAsync(string normalizedUsername);
    Task<ApplicationUser?> GetByEmailAsync(string normalizedEmail);
    Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName);
    Task<ApplicationUser> CreateAsync(ApplicationUser user);
    Task<ApplicationUser> UpdateAsync(ApplicationUser user);
    Task DeleteAsync(ApplicationUser user);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<bool> IsInRoleAsync(ApplicationUser user, string roleName);
    Task AddToRoleAsync(ApplicationUser user, string roleName);
    Task RemoveFromRoleAsync(ApplicationUser user, string roleName);
} 