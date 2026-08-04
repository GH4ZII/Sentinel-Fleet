using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Identity;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Ignore(t => t.IsActive);

        builder.HasIndex(t => t.UserId);
    }
}
