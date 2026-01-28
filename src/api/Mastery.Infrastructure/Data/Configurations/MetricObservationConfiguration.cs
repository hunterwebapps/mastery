using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class MetricObservationConfiguration : IEntityTypeConfiguration<MetricObservation>
{
    public void Configure(EntityTypeBuilder<MetricObservation> builder)
    {
        builder.ToTable("MetricObservations");

        builder.HasKey(x => x.Id);

        // Primary query pattern: user's observations for a metric by date
        builder.HasIndex(x => new { x.UserId, x.MetricDefinitionId, x.ObservedOn });

        // Cross-metric date query
        builder.HasIndex(x => new { x.MetricDefinitionId, x.ObservedOn });

        // User's daily observations
        builder.HasIndex(x => new { x.UserId, x.ObservedOn });

        // Correction lookup
        builder.HasIndex(x => x.CorrectedObservationId)
            .HasFilter("[CorrectedObservationId] IS NOT NULL");

        builder.Property(x => x.MetricDefinitionId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ObservedAt)
            .IsRequired();

        builder.Property(x => x.ObservedOn)
            .IsRequired();

        builder.Property(x => x.Value)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsCorrected)
            .IsRequired()
            .HasDefaultValue(false);

        // Foreign key to MetricDefinition
        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(x => x.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
