using DataBlocks.DataAdapters;
using Microsoft.AspNetCore.Identity;
using AuthBlocks.DataModels.Identity;

namespace AuthBlocksWeb.Identity.Stores;

public class CustomUserStore :
    IUserPasswordStore<ApplicationUser>, 
    IUserEmailStore<ApplicationUser>,
    IUserPhoneNumberStore<ApplicationUser>,
    IUserRoleStore<ApplicationUser>
{
    private readonly IDataAdapter<ApplicationUser> _userAdapter;
    private readonly IDataAdapter<ApplicationUserRole> _userRoleAdapter;
    private readonly IDataAdapter<ApplicationRole> _roleAdatper;

    public CustomUserStore(
        IDataAdapter<ApplicationUser> userAdapter, 
        IDataAdapter<ApplicationUserRole> userRoleAdapter, 
        IDataAdapter<ApplicationRole> roleAdatper)
    {
        _userAdapter = userAdapter;
        _userRoleAdapter = userRoleAdapter;
        _roleAdatper = roleAdatper;
    }

    // IUserStore implementation
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        // Insert user into your database
        await _userAdapter.Insert(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        // Delete user from your database
        await _userAdapter.Delete(user);
        return IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        // Find user by ID in your database
        if (!long.TryParse(userId, out long id)) throw new ArgumentException("Invalid user id");
        var result = await _userAdapter.GetByID(id);
        
        if (!result.Success) throw new ArgumentException("Failed to query user by ID");
        return result.Value;
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        // Find user by normalized username in your database
        var result = await _userAdapter.GetByPredicate(u => u.NormalizedUserName == normalizedUserName);
        if (!result.Success) throw new ArgumentException("Failed to query user by name");
        return result.Value?.FirstOrDefault();
    }

    // IUserPasswordStore implementation
    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash != null);
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    // IUserEmailStore implementation
    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var result = await _userAdapter.GetByPredicate(u => u.NormalizedEmail == normalizedEmail);
        if (!result.Success) throw new ArgumentException("Failed to query user by email");
        return result.Value?.FirstOrDefault();
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // Other IUserStore implementation
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await _userAdapter.Update(user);
        return await Task.FromResult(IdentityResult.Success);
    }
    

    public Task SetPhoneNumberAsync(ApplicationUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<string?> GetPhoneNumberAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        // _userAdapter?.Dispose();
    }

    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var rolesResult = await _roleAdatper.GetAll();
        if (!rolesResult.Success || rolesResult.Value is null) throw new Exception("Failed to query roles.");
        
        var userRolesResult = await _userRoleAdapter.GetByPredicate(userRole => userRole.UserId == user.Id);
        if (!userRolesResult.Success || userRolesResult.Value is null) throw new Exception("Failed to query user roles.");

        var roleIds = userRolesResult.Value.Select(userRole => userRole.RoleId).ToList();
        return rolesResult.Value
            .Where(role => roleIds.Contains(role.Id) && role.Name != null)
            .Select(role => role.Name!)
            .ToList();
    }

    public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var rolesResult = await _roleAdatper.GetByPredicate(role => role.Name == roleName);
        if (!rolesResult.Success || rolesResult.Value is null) throw new Exception("Failed to query roles.");
        
        var role = rolesResult.Value.FirstOrDefault();
        if (role is null) throw new Exception($"Failed to find role {roleName}.");
        
        var userRolesResult = await _userRoleAdapter.GetByPredicate(userRole => userRole.UserId == user.Id && userRole.RoleId == role.Id);
        if (!userRolesResult.Success || userRolesResult.Value is null) throw new Exception("Failed to query user roles.");
        
        return userRolesResult.Value.Any();
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var rolesResult = await _roleAdatper.GetByPredicate(role => role.Name == roleName);
        if (!rolesResult.Success || rolesResult.Value is null) throw new Exception("Failed to query roles.");
        
        var role = rolesResult.Value.FirstOrDefault();
        if (role is null) throw new Exception($"Failed to find role {roleName}.");
        
        var userRolesResult = await _userRoleAdapter.GetByPredicate(userRole => userRole.RoleId == role.Id);
        if (!userRolesResult.Success || userRolesResult.Value is null) throw new Exception("Failed to query user roles.");
        
        var userRoles = userRolesResult.Value.ToList();
        var userRoleIds = userRoles.Select(userRole => userRole.UserId).ToList();
        var usersResult = await _userAdapter.GetWhereIn(user => user.Id, userRoleIds);
        if (!usersResult.Success || usersResult.Value is null) throw new Exception("Failed to query user.");
        
        return usersResult.Value.ToList();
    }
}