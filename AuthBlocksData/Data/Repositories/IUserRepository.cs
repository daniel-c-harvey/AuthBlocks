using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;

namespace AuthBlocksData.Data.Repositories;

public interface IUserRepository : IRepository<ApplicationUser>
{
    // Identity-specific methods
    Task<ApplicationUser?> GetByUsernameAsync(string username);
    Task<ApplicationUser?> GetByEmailAsync(string email);
} 