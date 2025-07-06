using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
    {
        builder.ToTable("user_roles", "identity");
        
        // Configure primary key (custom - we're using Id instead of composite key)
        builder.HasKey(ur => ur.Id);
        
        // Configure inherited properties from IdentityUserRole<long>
        builder.Property(ur => ur.UserId)
            .IsRequired();
            
        builder.Property(ur => ur.RoleId)
            .IsRequired();
            
        // Configure additional custom properties
        builder.Property(ur => ur.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(ur => ur.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(ur => ur.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(ur => ur.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("ix_user_roles_userid");
            
        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("ix_user_roles_roleid");
            
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .HasDatabaseName("ix_user_roles_userid_roleid")
            .IsUnique();
            
        builder.HasIndex(ur => ur.Deleted)
            .HasDatabaseName("ix_user_roles_deleted");

        // Foreign keys
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
} 