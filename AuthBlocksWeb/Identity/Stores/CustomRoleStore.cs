using DataBlocks.DataAdapters;
using Microsoft.AspNetCore.Identity;
using AuthBlocks.DataModels.Identity;

namespace AuthBlocksWeb.Identity.Stores;

public class CustomRoleStore : IRoleStore<ApplicationRole>
{
    private readonly IDataAdapter<ApplicationRole> _roleAdapter;

    public CustomRoleStore(IDataAdapter<ApplicationRole> dataAdapter)
    {
        _roleAdapter = dataAdapter;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        await _roleAdapter.Insert(role);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        await _roleAdapter.Delete(role);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
        // _roleAdapter.Dispose();
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(roleId, out var id)) throw new ArgumentException("Invalid role id", nameof(roleId));
        var result = await _roleAdapter.GetByID(id);
        
        if (!result.Success) throw new Exception("Failed to query role by ID");
        return result.Value;
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        var result = await _roleAdapter.GetByPredicate(role => role.NormalizedName == normalizedRoleName);
        if (!result.Success) throw new Exception("Failed to query role by name");
        return result.Value?.FirstOrDefault();
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.NormalizedName);
    }

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id.ToString());
    }

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        await _roleAdapter.Update(role);
        return IdentityResult.Success;
    }
}
