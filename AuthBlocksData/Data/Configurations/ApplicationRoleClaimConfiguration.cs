using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
    {
        builder.ToTable("role_claims");
        
        // Configure primary key
        builder.HasKey(rc => rc.Id);
        
        // Configure inherited properties from IdentityRoleClaim<long>
        builder.Property(rc => rc.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(rc => rc.RoleId)
            .IsRequired();
            
        builder.Property(rc => rc.ClaimType);
            
        builder.Property(rc => rc.ClaimValue);
        
        // Configure additional custom properties
        builder.Property(rc => rc.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(rc => rc.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(rc => rc.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(rc => rc.RoleId)
            .HasDatabaseName("ix_role_claims_roleid");
            
        builder.HasIndex(rc => rc.Deleted)
            .HasDatabaseName("ix_role_claims_deleted");

        // Foreign key
        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(rc => rc.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
} 