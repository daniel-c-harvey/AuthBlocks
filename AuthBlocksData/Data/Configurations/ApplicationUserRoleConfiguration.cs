using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
    {
        builder.ToTable("user_roles");
        
        // Configure primary key (custom - we're using Id instead of composite key)
        builder.HasKey(ur => ur.Id);
            
        // Configure additional custom properties
        builder.Property(ur => ur.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(ur => ur.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(ur => ur.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(ur => ur.UpdatedAt)
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
            
        builder.HasIndex(ur => ur.IsDeleted)
            .HasDatabaseName("ix_user_roles_deleted");

        // Foreign keys
        builder.HasOne<ApplicationUser>(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne<ApplicationRole>(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
} 