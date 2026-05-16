using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthBlocksData.Data.Repositories;

public class UserRoleRepository : Repository<AuthDbContext, ApplicationUserRole>, IUserRoleRepository
{
    private readonly DbSet<ApplicationRole> _roles;

    public UserRoleRepository(AuthDbContext context, ILogger<Repository<AuthDbContext, ApplicationUserRole>> logger)
        : base(
            context,
            logger,
            q => q
                .Include(ur => ur.Role)
                .Include(ur => ur.User))
    {
        _roles = context.Roles;
    }

    protected override void UpdateEntity(ApplicationUserRole target, ApplicationUserRole source)
    {
        base.UpdateEntity(target, source);
        target.UserId = source.UserId;
        target.RoleId = source.RoleId;
    }
    
    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
        return await Query
            .Where(x => !x.User.IsDeleted && !x.Role.IsDeleted && x.Role.Name == roleName)
            .Select(x => x.User)
            .ToListAsync();
    }
    
    public async Task<IList<ApplicationRole>> GetRolesAsync(ApplicationUser user)
    {
        return await GetRolesAsync(user.Id);
    }

    public async Task<IList<ApplicationRole>> GetRolesAsync(long userId)
    {
        return await Query
            .Where(ur => ur.UserId == userId && !ur.Role.IsDeleted)
            .Select(ur => ur.Role)
            .ToListAsync();
    }


    public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName)
    {
        return await Query
            .Where(ur => ur.UserId == user.Id && !ur.User.IsDeleted && 
                                         ur.Role.Name == roleName && !ur.Role.IsDeleted)
            .AnyAsync();
    }

    public async Task AddToRoleAsync(ApplicationUser user, string roleName)
    {
        
        
        var role = await _roles.FirstOrDefaultAsync(r => r.Name == roleName && !r.IsDeleted);
        if (role == null)
            throw new InvalidOperationException($"Role '{roleName}' not found.");

        var existingUserRole = await Set
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

        if (existingUserRole == null)
        {
            var userRole = new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Set.Add(userRole);
        }
        else if (existingUserRole.IsDeleted)
        {
            existingUserRole.IsDeleted = false;
            existingUserRole.UpdatedAt = DateTime.UtcNow;
            Set.Update(existingUserRole);
        }

        await SaveChangesAsync();
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName)
    {
        var role = await _roles.FirstOrDefaultAsync(r => r.Name == roleName && !r.IsDeleted);
        if (role == null)
            return;

        var userRole = await Set
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id && !ur.IsDeleted);

        if (userRole != null)
        {
            userRole.IsDeleted = true;
            userRole.UpdatedAt = DateTime.UtcNow;
            Set.Update(userRole);
            await SaveChangesAsync();
        }
    }
}