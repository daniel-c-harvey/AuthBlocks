using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("roles", "identity");
        
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
        
        // Configure additional custom properties
        builder.Property(r => r.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(r => r.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(r => r.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(r => r.NormalizedName)
            .HasDatabaseName("ix_roles_normalizedname")
            .IsUnique();
            
        builder.HasIndex(r => r.Deleted)
            .HasDatabaseName("ix_roles_deleted");
    }
} 