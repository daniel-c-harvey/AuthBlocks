using AuthBlocksModels.Entities.Identity;
using Data.Data.Repositories;

namespace AuthBlocksData.Data.Repositories;

public interface IRoleRepository : IRepository<ApplicationRole>
{
    Task<ApplicationRole?> GetByNameAsync(string normalizedName);
} 