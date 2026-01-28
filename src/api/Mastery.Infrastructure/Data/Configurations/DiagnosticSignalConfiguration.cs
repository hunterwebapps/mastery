using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class DiagnosticSignalConfiguration : IEntityTypeConfiguration<DiagnosticSignal>
{
    public void Configure(EntityTypeBuilder<DiagnosticSignal> builder)
    {
        builder.ToTable("DiagnosticSignals");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.Type });
        builder.HasIndex(x => new { x.UserId, x.IsActive });

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Severity)
            .IsRequired();

        builder.OwnsOne(x => x.Evidence, evidence =>
        {
            evidence.Property(e => e.Metric)
                .HasColumnName("EvidenceMetric")
                .IsRequired()
                .HasMaxLength(200);

            evidence.Property(e => e.CurrentValue)
                .HasColumnName("EvidenceCurrentValue")
                .HasPrecision(18, 4);

            evidence.Property(e => e.ThresholdValue)
                .HasColumnName("EvidenceThresholdValue")
                .HasPrecision(18, 4);

            evidence.Property(e => e.Detail)
                .HasColumnName("EvidenceDetail")
                .HasMaxLength(1000);
        });

        builder.Property(x => x.DetectedOn)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ResolvedByRecommendationId);

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);

        builder.Ignore(x => x.DomainEvents);
    }
}
