using Microsoft.AspNetCore.Identity;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksData.Data.Repositories;

namespace AuthBlocksData.Services;

public class RoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IRoleRepository _roleRepository;

    public RoleService(RoleManager<ApplicationRole> roleManager, IRoleRepository roleRepository)
    {
        _roleManager = roleManager;
        _roleRepository = roleRepository;
    }

    // Standard Identity operations - use RoleManager
    public async Task<IdentityResult> CreateRoleAsync(ApplicationRole role)
    {
        return await _roleManager.CreateAsync(role);
    }

    public async Task<IdentityResult> UpdateRoleAsync(ApplicationRole role)
    {
        return await _roleManager.UpdateAsync(role);
    }

    public async Task<ApplicationRole?> FindByNameAsync(string roleName)
    {
        return await _roleManager.FindByNameAsync(roleName);
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }

    // Custom operations with soft delete - use Repository
    public async Task<ApplicationRole?> GetActiveRoleByIdAsync(long id)
    {
        return await _roleRepository.GetByIdAsync(id);
    }

    public async Task<ApplicationRole?> GetActiveRoleByNameAsync(string normalizedName)
    {
        return await _roleRepository.GetByNameAsync(normalizedName);
    }

    public async Task SoftDeleteRoleAsync(ApplicationRole role)
    {
        await _roleRepository.DeleteAsync(role);
    }
} 