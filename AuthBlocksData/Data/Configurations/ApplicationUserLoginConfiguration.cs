using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthBlocksModels.Entities.Identity;

namespace AuthBlocksData.Data.Configurations;

public class ApplicationUserLoginConfiguration : IEntityTypeConfiguration<ApplicationUserLogin>
{
    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder)
    {
        builder.ToTable("user_logins");
        
        // Configure primary key (custom - we're using Id instead of composite key)
        builder.HasKey(ul => ul.Id);
        
        // Configure inherited properties from IdentityUserLogin<long>
        builder.Property(ul => ul.LoginProvider)
            .HasMaxLength(128)
            .IsRequired();
            
        builder.Property(ul => ul.ProviderKey)
            .HasMaxLength(128)
            .IsRequired();
            
        builder.Property(ul => ul.ProviderDisplayName);
            
        builder.Property(ul => ul.UserId)
            .IsRequired();
            
        // Configure additional custom properties
        builder.Property(ul => ul.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(ul => ul.Deleted)
            .HasDefaultValue(false)
            .IsRequired();
            
        builder.Property(ul => ul.Created)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
            
        builder.Property(ul => ul.Modified)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        builder.HasIndex(ul => ul.UserId)
            .HasDatabaseName("ix_user_logins_userid");
            
        builder.HasIndex(ul => new { ul.LoginProvider, ul.ProviderKey })
            .HasDatabaseName("ix_user_logins_loginprovider_providerkey")
            .IsUnique();
            
        builder.HasIndex(ul => ul.Deleted)
            .HasDatabaseName("ix_user_logins_deleted");

        // Foreign key
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(ul => ul.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
} 