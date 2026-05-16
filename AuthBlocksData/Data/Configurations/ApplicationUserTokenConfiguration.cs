using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserTokenConfiguration : IEntityTypeConfiguration<ApplicationUserToken>
{
    public void Configure(EntityTypeBuilder<ApplicationUserToken> builder)
    {
        builder.ToTable("user_tokens");
        
        // Configure primary key (custom - we're using Id instead of composite key)
        builder.HasKey(ut => ut.Id);
        
        // Configure inherited properties from IdentityUserToken<long>
        builder.Property(ut => ut.UserId)
            .IsRequired();
            
        builder.Property(ut => ut.LoginProvider)
            .HasMaxLength(128)
            .IsRequired();
            
        builder.Property(ut => ut.Name)
            .HasMaxLength(128)
            .IsRequired();
            
        builder.Property(ut => ut.Value);
            
        // Configure additional custom properties
        builder.Property(ut => ut.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(ut => ut.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(ut => ut.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(ut => ut.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(ut => ut.UserId)
            .HasDatabaseName("ix_user_tokens_userid");
            
        builder.HasIndex(ut => new { ut.UserId, ut.LoginProvider, ut.Name })
            .HasDatabaseName("ix_user_tokens_userid_loginprovider_name")
            .IsUnique();
            
        builder.HasIndex(ut => ut.Deleted)
            .HasDatabaseName("ix_user_tokens_deleted");

        // Foreign key
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
} 