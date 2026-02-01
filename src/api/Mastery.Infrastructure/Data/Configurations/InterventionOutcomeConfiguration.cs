using Mastery.Domain.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class InterventionOutcomeConfiguration : IEntityTypeConfiguration<InterventionOutcome>
{
    public void Configure(EntityTypeBuilder<InterventionOutcome> builder)
    {
        builder.ToTable("InterventionOutcomes");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.RecommendationId);
        builder.HasIndex(x => new { x.UserId, x.RecommendationType });
        builder.HasIndex(x => new { x.UserId, x.ContextKey });

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.RecommendationId)
            .IsRequired();

        builder.Property(x => x.RecommendationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.InterventionCode)
            .HasMaxLength(50);

        builder.Property(x => x.ContextKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.WasAccepted)
            .IsRequired();

        builder.Property(x => x.WasDismissed)
            .IsRequired();

        builder.Property(x => x.DismissReason)
            .HasMaxLength(500);

        builder.Property(x => x.OriginalScore)
            .HasPrecision(10, 4);

        builder.Property(x => x.CapacityUtilization)
            .HasPrecision(5, 2);

        builder.Property(x => x.RecordedAt)
            .IsRequired();

        builder.Ignore(x => x.DomainEvents);
    }
}
