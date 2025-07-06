using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users", "identity");
        
        // Configure primary key
        builder.HasKey(u => u.Id);
        
        // Configure inherited properties from IdentityUser<long>
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();
            
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
        
        // Configure additional custom properties
        builder.Property(u => u.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(u => u.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(u => u.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.NormalizedUserName)
            .HasDatabaseName("ix_users_normalizedusername")
            .IsUnique();
            
        builder.HasIndex(u => u.NormalizedEmail)
            .HasDatabaseName("ix_users_normalizedemail");
            
        builder.HasIndex(u => u.Deleted)
            .HasDatabaseName("ix_users_deleted");
    }
} 