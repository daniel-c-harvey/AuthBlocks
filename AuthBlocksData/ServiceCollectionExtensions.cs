using AuthBlocksData.Data;
using AuthBlocksData.Data.Repositories;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthBlocksData;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AuthBlocks data layer services configured for Web API scenarios (without SignInManager)
    /// </summary>
    public static IServiceCollection AddAuthBlocksDataForWebApi(this IServiceCollection services, string connectionString)
    {
        // Add Entity Framework
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.SetPostgresVersion(18, 3);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            }));

        // Add Identity with Entity Framework stores for API scenarios
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false; // API doesn't use email confirmation workflow
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();

        // Don't add SignInManager for API scenarios - we use JWT instead

        // Add repositories for custom business logic
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IPendingRegistrationRepository, PendingRegistrationRepository>();

        // Add services that combine Identity managers with repositories
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IPendingRegistrationService, PendingRegistrationService>();
        services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

        return services;
    }
} 