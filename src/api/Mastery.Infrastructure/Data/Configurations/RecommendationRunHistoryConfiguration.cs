using Mastery.Domain.Entities.Recommendation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class RecommendationRunHistoryConfiguration : IEntityTypeConfiguration<RecommendationRunHistory>
{
    public void Configure(EntityTypeBuilder<RecommendationRunHistory> builder)
    {
        builder.ToTable("RecommendationRunHistory");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartedAt);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.UsersEvaluated)
            .IsRequired();

        builder.Property(x => x.UsersProcessed)
            .IsRequired();

        builder.Property(x => x.RecommendationsGenerated)
            .IsRequired();

        builder.Property(x => x.Errors)
            .IsRequired();

        builder.Property(x => x.ErrorDetails)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Ignore(x => x.DomainEvents);
    }
}
