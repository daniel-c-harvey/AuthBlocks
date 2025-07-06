using Microsoft.AspNetCore.Identity;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksData.Data.Repositories;

namespace AuthBlocksData.Services;

public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;

    public UserService(UserManager<ApplicationUser> userManager, IUserRepository userRepository)
    {
        _userManager = userManager;
        _userRepository = userRepository;
    }

    // Standard Identity operations - use UserManager
    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> FindByNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName);
    }

    // Custom operations with soft delete - use Repository
    public async Task<ApplicationUser?> GetActiveUserByIdAsync(long id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<ApplicationUser?> GetActiveUserByEmailAsync(string normalizedEmail)
    {
        return await _userRepository.GetByEmailAsync(normalizedEmail);
    }

    public async Task<IList<ApplicationUser>> GetActiveUsersInRoleAsync(string roleName)
    {
        return await _userRepository.GetUsersInRoleAsync(roleName);
    }

    public async Task SoftDeleteUserAsync(ApplicationUser user)
    {
        await _userRepository.DeleteAsync(user);
    }

    // Role operations with soft delete
    public async Task<IList<string>> GetActiveUserRolesAsync(ApplicationUser user)
    {
        return await _userRepository.GetRolesAsync(user);
    }

    public async Task AddUserToRoleAsync(ApplicationUser user, string roleName)
    {
        await _userRepository.AddToRoleAsync(user, roleName);
    }

    public async Task RemoveUserFromRoleAsync(ApplicationUser user, string roleName)
    {
        await _userRepository.RemoveFromRoleAsync(user, roleName);
    }
} 