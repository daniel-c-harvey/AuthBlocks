using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AuthBlocksWeb.Components.Account;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksData.Data;
using AuthBlocksData.Services;

namespace AuthBlocksWeb;

public static class Startup
{
    private static readonly ApplicationRole[] SYSTEM_ROLES = 
    [
        new ApplicationRole()
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture)
        }
    ];

    public static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddCascadingAuthenticationState()
            .AddScoped<IdentityUserAccessor>()
            .AddScoped<IdentityRedirectManager>()
            .AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        // Add authentication with Identity cookies
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies();

        // Add AuthBlocks data layer with EF and Identity
        // services.AddAuthBlocksData(connectionString);

        // Configure authentication cookies
        services.ConfigureApplicationCookie(ConfigureAuthCookie);
        
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

        services.AddAuthorizationCore();
    }

    public static async Task ConfigureAppAsync(WebApplication app)
    {
        // Ensure database is created
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Add system roles using the RoleService
        var roleService = scope.ServiceProvider.GetRequiredService<RoleService>();
        await AddSystemRolesAsync(roleService);
    }

    private static async Task AddSystemRolesAsync(RoleService roleService)
    {
        foreach (ApplicationRole role in SYSTEM_ROLES)
        {
            var existingRole = await roleService.FindByNameAsync(role.Name!);
            if (existingRole == null)
            {
                await roleService.CreateRoleAsync(role);
            }
        }
    }

    private static void ConfigureAuthCookie(CookieAuthenticationOptions options)
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    }
}

// Example usage in Program.cs:
/*
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
NewStartupExample.ConfigureServices(
    builder.Services, 
    builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Configure the HTTP request pipeline
await NewStartupExample.ConfigureAppAsync(app);

// Configure the app...
app.Run();
*/ 