using Microsoft.EntityFrameworkCore;
using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace AuthBlocksData.Data.Repositories;

public class RoleRepository : Repository<AuthDbContext, ApplicationRole>, IRoleRepository
{
    public RoleRepository(AuthDbContext context, ILogger<Repository<AuthDbContext, ApplicationRole>> logger) 
        : base(
            context, 
            logger, 
            q => q
                .Include(r => r.ParentRole) 
                .Include(r => r.ChildRoles))
    { }

    protected override void UpdateEntity(ApplicationRole target, ApplicationRole source)
    {
        base.UpdateEntity(target, source);
        target.ParentRoleId = source.ParentRoleId;
        target.Name = source.Name;
        target.NormalizedName = source.NormalizedName;
    }
    
    public async Task<ApplicationRole?> GetByNameAsync(string normalizedName)
    {
        return await Query
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName);
    }
} 