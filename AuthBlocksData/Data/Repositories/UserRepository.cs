using Microsoft.EntityFrameworkCore;
using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace AuthBlocksData.Data.Repositories;

public class UserRepository : Repository<AuthDbContext, ApplicationUser>, IUserRepository
{
    public UserRepository(AuthDbContext context, ILogger<UserRepository> logger) : base(context, logger)
    {
    }

    protected override void UpdateEntity(ApplicationUser target, ApplicationUser source)
    {
        base.UpdateEntity(target, source);
        target.IsDeactivated = source.IsDeactivated;
        target.UserName = source.UserName;
        target.Email = source.Email;
        target.EmailConfirmed = source.EmailConfirmed;
        target.PhoneNumber = source.PhoneNumber;
        target.PhoneNumberConfirmed = source.PhoneNumberConfirmed;
        target.LockoutEnd = source.LockoutEnd;
        target.LockoutEnabled = source.LockoutEnabled;
        target.AccessFailedCount = source.AccessFailedCount;
    }

    // Identity-specific methods
    public async Task<ApplicationUser?> GetByUsernameAsync(string username)
    {
        return await Query.FirstOrDefaultAsync(u => u.UserName == username);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await Query.FirstOrDefaultAsync(u => u.Email == email);
    }

    

    
} 