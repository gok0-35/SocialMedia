using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialMedia.Api.Domain.Entities;

namespace SocialMedia.Api.Infrastructure.Persistence.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(x => new { x.FollowerId, x.FollowingId });

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(x => x.FollowingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
