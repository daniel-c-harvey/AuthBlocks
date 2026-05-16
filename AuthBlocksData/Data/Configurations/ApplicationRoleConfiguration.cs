using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("roles");
        
        // Configure primary key
        builder.HasKey(r => r.Id);
        
        // Configure inherited properties from IdentityRole<long>
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(r => r.Name)
            .HasMaxLength(256);
            
        builder.Property(r => r.NormalizedName)
            .HasMaxLength(256);
            
        builder.Property(r => r.ConcurrencyStamp)
            .IsConcurrencyToken();
        
        // Configure hierarchy
        builder.Property(r => r.ParentRoleId)
            .IsRequired(false);
        
        // Configure additional custom properties
        builder.Property(r => r.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(r => r.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Configure hierarchy relationship
        builder.HasOne(r => r.ParentRole)
            .WithMany(r => r.ChildRoles)
            .HasForeignKey(r => r.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => r.NormalizedName)
            .HasDatabaseName("ix_roles_normalizedname")
            .IsUnique();
            
        builder.HasIndex(r => r.IsDeleted)
            .HasDatabaseName("ix_roles_deleted");
            
        builder.HasIndex(r => r.ParentRoleId)
            .HasDatabaseName("ix_roles_parentroleid");
    }
} 