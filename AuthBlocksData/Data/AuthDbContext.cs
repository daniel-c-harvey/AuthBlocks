using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long, 
    ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, 
    ApplicationRoleClaim, ApplicationUserToken>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from the assembly
        builder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
} 