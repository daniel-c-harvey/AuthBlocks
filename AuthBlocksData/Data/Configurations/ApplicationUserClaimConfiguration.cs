using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserClaimConfiguration : IEntityTypeConfiguration<ApplicationUserClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationUserClaim> builder)
    {
        builder.ToTable("user_claims");
        
        // Configure primary key
        builder.HasKey(uc => uc.Id);
        
        // Configure inherited properties from IdentityUserClaim<long>
        builder.Property(uc => uc.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(uc => uc.UserId)
            .IsRequired();
            
        builder.Property(uc => uc.ClaimType);
            
        builder.Property(uc => uc.ClaimValue);
        
        // Configure additional custom properties
        builder.Property(uc => uc.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(uc => uc.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(uc => uc.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(uc => uc.UserId)
            .HasDatabaseName("ix_user_claims_userid");
            
        builder.HasIndex(uc => uc.Deleted)
            .HasDatabaseName("ix_user_claims_deleted");

        // Foreign key
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
} 