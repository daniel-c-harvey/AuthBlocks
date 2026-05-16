using System.Globalization;
using AuthBlocksData.Data;
using AuthBlocksData.Services;
using AuthBlocksLib.Options;
using AuthBlocksModels.Models;
using AuthBlocksModels.SystemDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthBlocksLib;

/// <summary>
/// Post-build startup hook: applies EF migrations and seeds system roles (and optionally the admin user).
/// Call this once on <see cref="IApplicationBuilder.ApplicationServices"/> after <c>builder.Build()</c>.
/// </summary>
public static class AuthBlocksStartupExtensions
{
    public static async Task UseAuthBlocksStartupAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        // 1) Apply pending EF migrations on startup.
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.MigrateAsync();

        // 2) Seed system roles (always).
        await SeedSystemRolesAsync(scope.ServiceProvider);

        // 3) Seed admin user (only when configured — null means the host doesn't want admin seeding).
        var options = scope.ServiceProvider.GetRequiredService<AuthBlocksOptions>();
        if (options.AdminUserSettings is { } adminSettings)
        {
            await SeedAdminUserAsync(scope.ServiceProvider, adminSettings);
        }
    }

    private static async Task SeedSystemRolesAsync(IServiceProvider services)
    {
        var roleService = services.GetRequiredService<IRoleService>();

        // Hierarchical order: parents first so children can resolve their parent FK.
        foreach (var systemRole in SystemRole.GetAll().OrderBy(r => r.Id))
        {
            var existingRoleResult = await roleService.FindByNameAsync(systemRole.Name);
            if (existingRoleResult.Value != null) continue;

            var existingParentRole = systemRole.ParentRole is not null
                ? await roleService.FindByNameAsync(systemRole.ParentRole.Name)
                : null;

            var role = new RoleModel
            {
                Name = systemRole.Name,
                NormalizedName = systemRole.Name.ToUpperInvariant(),
                // Only the ID — full ParentRole object would trip EF tracking.
                ParentRole = existingParentRole?.Value != null
                    ? new RoleModel { Id = existingParentRole.Value.Id }
                    : null,
                ConcurrencyStamp = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await roleService.Add(role);
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider services, AdminUserSettings adminSettings)
    {
        ValidateAdminSettings(adminSettings);

        var userService = services.GetRequiredService<IUserService>();
        var userRoleService = services.GetRequiredService<IUserRoleService>();

        var existingUser = await userService.FindByNameAsync(adminSettings.UserName);
        if (existingUser is null)
        {
            var user = new UserModel
            {
                UserName = adminSettings.UserName,
                Email = adminSettings.Email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var createResult = await userService.Add(user, adminSettings.Password);
            if (createResult.Success && createResult.Value != null)
            {
                await userRoleService.AddUserToRoleAsync(createResult.Value, SystemRoleConstants.Admin);
            }
        }
        else if (existingUser.Email != adminSettings.Email)
        {
            existingUser.Email = adminSettings.Email;
            await userService.Update(existingUser);
        }
        else if (!await userService.CheckPassword(existingUser, adminSettings.Password))
        {
            await userService.UpdatePassword(existingUser, adminSettings.Password);
        }
    }

    private static void ValidateAdminSettings(AdminUserSettings adminSettings)
    {
        if (string.IsNullOrWhiteSpace(adminSettings.UserName))
            throw new InvalidOperationException("AdminUserSettings.UserName is required when admin seeding is enabled.");
        if (string.IsNullOrWhiteSpace(adminSettings.Email))
            throw new InvalidOperationException("AdminUserSettings.Email is required when admin seeding is enabled.");
        if (string.IsNullOrWhiteSpace(adminSettings.Password))
            throw new InvalidOperationException("AdminUserSettings.Password is required when admin seeding is enabled.");
    }
}
