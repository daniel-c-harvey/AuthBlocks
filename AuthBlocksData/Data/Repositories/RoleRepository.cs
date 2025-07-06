using Microsoft.EntityFrameworkCore;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationRole?> GetByIdAsync(long id)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.Deleted);
    }

    public async Task<ApplicationRole?> GetByNameAsync(string normalizedName)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedName && !r.Deleted);
    }

    public async Task<ApplicationRole> CreateAsync(ApplicationRole role)
    {
        role.Created = DateTime.UtcNow;
        role.Modified = DateTime.UtcNow;
        
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<ApplicationRole> UpdateAsync(ApplicationRole role)
    {
        role.Modified = DateTime.UtcNow;
        
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task DeleteAsync(ApplicationRole role)
    {
        role.Deleted = true;
        role.Modified = DateTime.UtcNow;
        
        _context.Roles.Update(role);
        await _context.SaveChangesAsync();
    }
} 