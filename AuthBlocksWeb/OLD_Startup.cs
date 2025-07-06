// using System.Globalization;
// using Microsoft.AspNetCore.Authentication.Cookies;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Components.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.Extensions.DependencyInjection;
// using AuthBlocksWeb.Components.Account;
//
// namespace AuthBlocksWeb;
//
// public static class OLD_Startup<TClient, TDatabase>
// {
//     private const string AUTH_COOKIE_NAME = ".TCBC.Auth";
//     private static readonly ApplicationRole[] SYSTEM_ROLES = 
//     [
//         new ApplicationRole()
//         {
//             Name = "Admin",
//             NormalizedName = "ADMIN",
//             ConcurrencyStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture)
//         }
//     ];
//
//     public static void ConfigureServices(IServiceCollection services, string connectionString, string databaseName)
//     {
//         IDataAccess<TDatabase> dataAccess = DataAccessFactory.Create<TClient, TDatabase>(connectionString, databaseName);
//         IQueryBuilder<TDatabase> queryBuilder = QueryBuilderFactory.Create<TDatabase>();
//         
//         DataAdapterPackage adapters = BuildAdapters(dataAccess, queryBuilder);
//
//         services
//             .AddCascadingAuthenticationState()
//             .AddScoped<IdentityUserAccessor>()
//             .AddScoped<IdentityRedirectManager>()
//             .AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>()
//             .AddSingleton(_ => adapters.UserAdapter)
//             .AddSingleton(_ => adapters.RoleAdapter)
//             .AddSingleton(_ => adapters.UserRoleAdapter)
//             // Add authentication
//             .AddAuthentication(options =>
//             {
//                 options.DefaultScheme = IdentityConstants.ApplicationScheme;
//                 options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//             })
//             .AddIdentityCookies();
//
//         services
//             .AddIdentityCore<ApplicationUser>((options) =>
//             {
//                 options.SignIn.RequireConfirmedAccount = true;
//                 options.Password.RequireNonAlphanumeric = false;
//                 options.Password.RequireUppercase = false;
//                 options.Password.RequireLowercase = false;
//                 options.Password.RequireDigit = false;
//             })
//             .AddRoles<ApplicationRole>()
//             .AddRoleStore<CustomRoleStore>()
//             .AddUserStore<CustomUserStore>()
//             .AddSignInManager()
//             .AddDefaultTokenProviders();
//
//    
//             services.ConfigureApplicationCookie(ConfigureAuthCookie);
//             // services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
//
//         services
//             .AddAuthorizationCore();
//     }
//
//     public static void ConfigureApp(WebApplication app)
//     {
//         var roleStore = app.Services.CreateScope().ServiceProvider.GetRequiredService<IRoleStore<ApplicationRole>>();
//         if (roleStore == null) throw new Exception("RoleStore not found");
//         
//         AddSystemRoles(roleStore);
//     }
//     private static async void AddSystemRoles(IRoleStore<ApplicationRole> roleStore)
//     {
//         foreach (ApplicationRole role in SYSTEM_ROLES)
//         {
//             var roleResult = await roleStore.FindByNameAsync(role.NormalizedName, CancellationToken.None);
//             if (roleResult == null)
//             {
//                 await roleStore.CreateAsync(role, CancellationToken.None);
//             }
//         }
//
//     }
//
//     private static DataAdapterPackage BuildAdapters(IDataAccess<TDatabase> dataAccess, IQueryBuilder<TDatabase> queryBuilder)
//     {
//         return new DataAdapterPackage()
//         {
//             UserAdapter = DataAdapterFactory.Create<TDatabase, ApplicationUser>(
//                 dataAccess,
//                 queryBuilder,
//                 DataSchema.Create<ApplicationUser>("users")
//             ),
//             RoleAdapter = DataAdapterFactory.Create<TDatabase, ApplicationRole>(
//                 dataAccess,
//                 queryBuilder,
//                 DataSchema.Create<ApplicationRole>("users")
//             ),
//             UserRoleAdapter = DataAdapterFactory.Create<TDatabase, ApplicationUserRole>(
//                 dataAccess,
//                 queryBuilder,
//                 DataSchema.Create<ApplicationUserRole>("users")
//             )
//         };
//     }
//
//     private static void ConfigureAuthCookie(CookieAuthenticationOptions options)
//     {
//         // options.Cookie.Name = AUTH_COOKIE_NAME;
//         // options.Cookie.HttpOnly = true;
//         // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//         // options.Cookie.SameSite = SameSiteMode.Strict;
//         options.ExpireTimeSpan = TimeSpan.FromDays(30);
//         options.SlidingExpiration = true;
//         // options.LoginPath = "/login";
//         // options.LogoutPath = "/logout";
//     }
//
//     private class DataAdapterPackage
//     {
//         public required IDataAdapter<ApplicationUser> UserAdapter { get; set; }
//         public required IDataAdapter<ApplicationRole> RoleAdapter { get; set; }
//         public required IDataAdapter<ApplicationUserRole> UserRoleAdapter { get; set; }
//     }
// }
