using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuthBlocksData.Data.Repositories;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace AuthBlocksData.Data;

public static class ServiceCollectionExtensions
{
    // public static IServiceCollection AddAuthBlocksData(this IServiceCollection services, string connectionString)
    // {
    //     // Add Entity Framework
    //     services.AddDbContext<AuthDbContext>(options =>
    //         options.UseNpgsql(connectionString));
    //
    //     // Add Identity with Entity Framework stores
    //     services.AddIdentityCore<ApplicationUser>(options =>
    //     {
    //         options.SignIn.RequireConfirmedAccount = true;
    //         options.Password.RequireNonAlphanumeric = false;
    //         options.Password.RequireUppercase = false;
    //         options.Password.RequireLowercase = false;
    //         options.Password.RequireDigit = false;
    //     })
    //     .AddRoles<ApplicationRole>()
    //     .AddEntityFrameworkStores<AuthDbContext>()
    //     .AddDefaultTokenProviders();
    //
    //     // Add SignInManager manually
    //     services.AddScoped<SignInManager<ApplicationUser>>();
    //
    //     // Add repositories for custom business logic
    //     services.AddScoped<IUserRepository, UserRepository>();
    //     services.AddScoped<IRoleRepository, RoleRepository>();
    //
    //     // Add services that combine Identity managers with repositories
    //     services.AddScoped<UserService>();
    //     services.AddScoped<RoleService>();
    //
    //     return services;
    // }

    /// <summary>
    /// Adds AuthBlocks data layer services configured for Web API scenarios (without SignInManager)
    /// </summary>
    public static IServiceCollection AddAuthBlocksDataForWebApi(this IServiceCollection services, string connectionString)
    {
        // Add Entity Framework
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

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

        // Add services that combine Identity managers with repositories
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();

        return services;
    }
} 