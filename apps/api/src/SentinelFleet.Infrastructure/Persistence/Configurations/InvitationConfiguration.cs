using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email).HasMaxLength(320).IsRequired();
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(i => i.Token).HasMaxLength(128).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => new { i.OrganizationId, i.Email });

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
