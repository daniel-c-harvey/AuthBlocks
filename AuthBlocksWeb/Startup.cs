using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using AuthBlocksWeb.ApiClients;
using AuthBlocksWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using AuthBlocksModels.Entities.Identity;
using AuthBlocksWeb.Components.Pages.UserAdmin;
using AuthBlocksWeb.Components.Pages.UserAdmin.Permissions;
using AuthBlocksWeb.Components.Pages.UserAdmin.Registrations;
using AuthBlocksWeb.Components.Pages.UserAdmin.Users;
using AuthBlocksWeb.HierarchicalAuthorize;
using Web;

namespace AuthBlocksWeb;

public static class Startup
{
    public static void ConfigureAuthServices(IServiceCollection services, string apiBaseUrl)
    {
        // Add Blazor authentication state management
        services.AddCascadingAuthenticationState();

        // Add custom JWT-based authentication services.
        // ITokenStore (pure storage) terminates the dep chain — it has no auth-API
        // or cascade dependency — so AuthSession → ISessionExpiredAction →
        // JwtAuthenticationStateProvider → ITokenStore composes without a cycle.
        services.AddScoped<ITokenStore, TokenStore>();
        services.AddScoped<JwtAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddScoped<ISessionExpiredAction>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddScoped<IAuthSession, AuthSession>();

        // Registers EditModalSaveContextHolder (scoped) required by the ModelView-based pages
        // (Users, Registrations). Delegates to BlazorBlocks.Web's own registration extension
        // so the registration stays in sync with the library's lifetime expectations.
        services.AddBlazorBlocksWeb();

        services.AddSingleton(new AuthClientConfig(apiBaseUrl));
        services.AddScoped<IAuthApiClient, AuthApiClient>();
        
        // Register the hierarchical role service and authorization handlers
        services.AddScoped<IHierarchicalRoleService, HierarchicalRoleService>();
        services.AddScoped<IAuthorizationHandler, HierarchicalRolesAuthorizationHandler>();
        
        // Add authorization with hierarchical role support
        services.AddAuthentication().AddCookie(IdentityConstants.BearerScheme);
        services.AddAuthorization();
        
        // Register client configs and clients
        // User Client
        services.AddSingleton(new UsersClientConfig(apiBaseUrl));
        services.AddScoped<UsersClient>();
        services.AddScoped<UsersViewModel>();
        
        // Roles Client
        services.AddSingleton(new RolesClientConfig(apiBaseUrl));
        services.AddScoped<RoleClient>();
        // builderServices.AddScoped<RolesViewModel>();
        
        // User Roles Client
        services.AddSingleton(new UserRolesClientConfig(apiBaseUrl));
        services.AddScoped<IUserRolesClient, UserRolesClient>();
        services.AddScoped<UserRolesClient>();
        services.AddScoped<PermissionsViewModel>();

        // Pending Registration Client
        services.AddSingleton(new PendingRegistrationClientConfig(apiBaseUrl));
        services.AddScoped<PendingRegistrationClient>();
        services.AddScoped<RegistrationsViewModel>();
    }
}