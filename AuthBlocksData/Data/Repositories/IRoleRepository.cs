using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Repositories;

public interface IRoleRepository
{
    Task<ApplicationRole?> GetByIdAsync(long id);
    Task<ApplicationRole?> GetByNameAsync(string normalizedName);
    Task<ApplicationRole> CreateAsync(ApplicationRole role);
    Task<ApplicationRole> UpdateAsync(ApplicationRole role);
    Task DeleteAsync(ApplicationRole role);
} 