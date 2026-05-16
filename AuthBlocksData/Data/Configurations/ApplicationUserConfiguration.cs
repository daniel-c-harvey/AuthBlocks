using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;
using Data.Shared.Data.Configurations;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserConfiguration : BaseEntityConfiguration<ApplicationUser>
{
    public override void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");
        
        // Call base configuration first (handles IEntity properties)
        base.Configure(builder);
            
        builder.Property(u => u.UserName)
            .HasMaxLength(256);
            
        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(256);
            
        builder.Property(u => u.Email)
            .HasMaxLength(256);
            
        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(256);
            
        builder.Property(u => u.EmailConfirmed)
            .IsRequired();
            
        builder.Property(u => u.PasswordHash);
            
        builder.Property(u => u.SecurityStamp);
            
        builder.Property(u => u.ConcurrencyStamp)
            .IsConcurrencyToken();
            
        builder.Property(u => u.PhoneNumber);
            
        builder.Property(u => u.PhoneNumberConfirmed)
            .IsRequired();
            
        builder.Property(u => u.TwoFactorEnabled)
            .IsRequired();
            
        builder.Property(u => u.LockoutEnd);
            
        builder.Property(u => u.LockoutEnabled)
            .IsRequired();
            
        builder.Property(u => u.AccessFailedCount)
            .IsRequired();

        builder.Property(u => u.IsDeactivated)
            .HasDefaultValue(false);

        // Identity-specific indexes
        builder.HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName("ix_users_normalizedusername")
            .IsUnique();
            
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("ix_users_normalizedemail");
    }
} 