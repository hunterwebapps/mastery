using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Task;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class TaskMetricBindingConfiguration : IEntityTypeConfiguration<TaskMetricBinding>
{
    public void Configure(EntityTypeBuilder<TaskMetricBinding> builder)
    {
        builder.ToTable("TaskMetricBindings");

        builder.HasKey(x => x.Id);

        // Index on TaskId for loading task's bindings
        builder.HasIndex(x => x.TaskId);

        // Index on MetricDefinitionId for finding tasks contributing to a metric
        builder.HasIndex(x => x.MetricDefinitionId);

        // Unique constraint: same metric can't be bound to same task twice
        builder.HasIndex(x => new { x.TaskId, x.MetricDefinitionId }).IsUnique();

        builder.Property(x => x.TaskId)
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
            .OnDelete(DeleteBehavior.Restrict); // Don't delete metric definition if bound to tasks

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);
    }
}
