using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class HabitMetricBindingConfiguration : IEntityTypeConfiguration<HabitMetricBinding>
{
    public void Configure(EntityTypeBuilder<HabitMetricBinding> builder)
    {
        builder.ToTable("HabitMetricBindings");

        builder.HasKey(x => x.Id);

        // Index on HabitId for loading habit's bindings
        builder.HasIndex(x => x.HabitId);

        // Index on MetricDefinitionId for finding habits contributing to a metric
        builder.HasIndex(x => x.MetricDefinitionId);

        // Unique constraint: same metric can't be bound to same habit twice
        builder.HasIndex(x => new { x.HabitId, x.MetricDefinitionId }).IsUnique();

        builder.Property(x => x.HabitId)
            .IsRequired();

        builder.Property(x => x.MetricDefinitionId)
            .IsRequired();

        builder.Property(x => x.ContributionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.FixedValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Foreign key to MetricDefinition
        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(x => x.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete metric definition if bound to habits

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);
    }
}
