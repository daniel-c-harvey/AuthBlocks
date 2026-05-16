using AuthBlocksModels.Entities;
using Data.Shared.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthBlocksData.Data.Configurations;

public class PendingRegistrationConfiguration : BaseEntityConfiguration<PendingRegistration>
{
    public override void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("pending_registrations");
        
        base.Configure(builder);

        builder.Property(x => x.PendingUserEmail)
            .IsRequired();
        
        builder.Property(x => x.ExpiresAt)
            .IsRequired();
        
        builder.Property(x => x.TokenHash)
            .IsRequired();
        
        builder.Property(x => x.IsConsumed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ConsumedAt);

        builder.Ignore(x => x.Roles);
        builder.Property(x => x.RoleIds);

        builder.HasIndex(x => new { x.PendingUserEmail, x.IsConsumed });

    }
}