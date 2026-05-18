using System.Text;
using API.Common.Email.Mailtrap;
using AuthBlocksData;
using AuthBlocksLib.HierarchicalAuthorize;
using AuthBlocksLib.Models;
using AuthBlocksLib.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetBlocks.Models.Environment;

namespace AuthBlocksLib;

/// <summary>
/// Fluent registration surface for hosts that want to mount AuthBlocks.
/// </summary>
public static class AuthBlocksExtensions
{
    /// <summary>
    /// Register every AuthBlocks dependency the host needs.
    /// </summary>
    public static IServiceCollection AddAuthBlocks(this IServiceCollection services, Action<AuthBlocksOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AuthBlocksOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("AuthBlocksOptions.ConnectionString is required.");
        if (string.IsNullOrWhiteSpace(options.ApplicationName))
            throw new InvalidOperationException("AuthBlocksOptions.ApplicationName is required.");
        if (string.IsNullOrWhiteSpace(options.JwtSettings.Secret))
            throw new InvalidOperationException("AuthBlocksOptions.JwtSettings.Secret is required.");
        if (options.JwtSettings.Secret.Length < 32)
            throw new InvalidOperationException("AuthBlocksOptions.JwtSettings.Secret must be at least 32 characters long.");
        if (string.IsNullOrWhiteSpace(options.JwtSettings.Issuer))
            throw new InvalidOperationException("AuthBlocksOptions.JwtSettings.Issuer is required.");
        if (string.IsNullOrWhiteSpace(options.JwtSettings.Audience))
            throw new InvalidOperationException("AuthBlocksOptions.JwtSettings.Audience is required.");
        if (string.IsNullOrWhiteSpace(options.EmailConnection.Host))
            throw new InvalidOperationException("AuthBlocksOptions.EmailConnection.Host is required.");
        if (string.IsNullOrWhiteSpace(options.EmailConnection.Token))
            throw new InvalidOperationException("AuthBlocksOptions.EmailConnection.Token is required.");

        // Expose the populated options so post-build startup (migrations + seeding) can read them.
        services.AddSingleton(options);
        services.Configure<AuthBlocksOptions>(o =>
        {
            o.ConnectionString = options.ConnectionString;
            o.ApplicationName = options.ApplicationName;
            o.SupportEmail = options.SupportEmail;
            o.JwtSettings = options.JwtSettings;
            o.EmailConnection = options.EmailConnection;
            o.AdminUserSettings = options.AdminUserSettings;
        });

        // Data layer
        services.AddAuthBlocksDataForWebApi(options.ConnectionString);

        // JWT settings — both as a directly-injectable singleton and via IOptions<>.
        services.AddSingleton(options.JwtSettings);
        services.Configure<JwtSettings>(jwt =>
        {
            jwt.Secret = options.JwtSettings.Secret;
            jwt.Issuer = options.JwtSettings.Issuer;
            jwt.Audience = options.JwtSettings.Audience;
            jwt.ExpiryMinutes = options.JwtSettings.ExpiryMinutes;
            jwt.RefreshTokenExpiryDays = options.JwtSettings.RefreshTokenExpiryDays;
        });

        // JWT + auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRegistrationTokenService, RegistrationTokenService>();

        services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = options.JwtSettings.Issuer,
                    ValidAudience = options.JwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        // Hierarchical role authorization
        services.AddSingleton<HierarchicalRoleCache>();
        services.AddScoped<IHierarchicalRoleService, HierarchicalRoleService>();
        services.AddScoped<IAuthorizationHandler, HierarchicalRolesAuthorizationHandler>();

        // Email
        var emailConnection = options.EmailConnection;
        services.AddSingleton<IOptions<EmailConnection>>(_ => new OptionsWrapper<EmailConnection>(emailConnection));
        services.AddScoped<IGeneralEmailSender, MailtrapEmailSender>();

        return services;
    }
}
