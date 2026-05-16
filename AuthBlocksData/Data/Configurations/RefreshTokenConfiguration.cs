using AuthBlocksData.Data.Entities;
using AuthBlocksModels.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBlocksData.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TokenHash)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.TokenHash)
            .IsUnique();

        builder.HasIndex(e => e.ExpiresAt);

        builder.HasIndex(e => new { e.UserId, e.ExpiresAt })
            .HasDatabaseName("IX_refresh_tokens_UserId_ExpiresAt");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
