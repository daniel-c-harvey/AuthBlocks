using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;

namespace AuthBlocksData.Data.Repositories;

public interface IRoleRepository : IRepository<ApplicationRole>
{
    Task<ApplicationRole?> GetByNameAsync(string normalizedName);
} 