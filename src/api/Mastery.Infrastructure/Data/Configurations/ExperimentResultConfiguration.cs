using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class ExperimentResultConfiguration : IEntityTypeConfiguration<ExperimentResult>
{
    public void Configure(EntityTypeBuilder<ExperimentResult> builder)
    {
        builder.ToTable("ExperimentResults");

        builder.HasKey(x => x.Id);

        // Unique index for one-to-one relationship
        builder.HasIndex(x => x.ExperimentId)
            .IsUnique();

        builder.Property(x => x.ExperimentId)
            .IsRequired();

        builder.Property(x => x.BaselineValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.RunValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.Delta)
            .HasPrecision(18, 4);

        builder.Property(x => x.DeltaPercent)
            .HasPrecision(18, 4);

        builder.Property(x => x.OutcomeClassification)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ComplianceRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.NarrativeSummary)
            .HasMaxLength(4000);

        builder.Property(x => x.ComputedAt)
            .IsRequired();

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}
