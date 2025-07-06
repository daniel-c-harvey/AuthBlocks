using AuthBlocksAPI.Models;
using AuthBlocksAPI.Services;
using AuthBlocksData.Data;
using AuthBlocksData.Services;
using AuthBlocksModels.Entities.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NetBlocks.Models.Environment;
using NetBlocks.Utilities.Environment;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Configure JWT Settings
        var jwtSettings = new JwtSettings();
        builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

        // Add CORS
        var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorApp", policy =>
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Add AuthBlocks data layer
        var connection = LoadConnection();
        builder.Services.AddAuthBlocksDataForWebApi(connection.ConnectionString);

        // Add JWT Service
        builder.Services.AddScoped<IJwtService, JwtService>();

        // Add JWT Authentication
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowBlazorApp");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Initialize database and system data
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            await context.Database.EnsureCreatedAsync();
    
            // Add system roles
            await SeedSystemRolesAsync(scope.ServiceProvider);
        }

        app.Run();
        return;
    }
    private static Connection LoadConnection()
    {
        Connections? connections = ConnectionStringTools.LoadFromFile("environment/connections.json");
    
        if (connections == null) throw new Exception("No connections configuration found");

        Connection? connection = connections.ConnectionStrings
            .FirstOrDefault(c => c.ID == connections.ActiveConnectionID);
    
        if (connection == null) throw new Exception("Active connection not found");
    
        return connection;
    }
    
    private static JwtSettings LoadJwtConfig()
    {
        Connections? connections = ConnectionStringTools.LoadFromFile("environment/connections.json");
    
        if (connections == null) throw new Exception("No connections configuration found");

        Connection? connection = connections.ConnectionStrings
            .FirstOrDefault(c => c.ID == connections.ActiveConnectionID);
    
        if (connection == null) throw new Exception("Active connection not found");
    
        return connection;
    }

    static async Task SeedSystemRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleService = scope.ServiceProvider.GetRequiredService<RoleService>();

        var adminRole = new ApplicationRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = DateTime.UtcNow.ToString(),
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        var existingRole = await roleService.FindByNameAsync("Admin");
        if (existingRole == null)
        {
            await roleService.CreateRoleAsync(adminRole);
        }
    
    }
}
