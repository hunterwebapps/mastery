using System.Text.Json;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mastery.Infrastructure.Data.Configurations;

public class GoalMetricConfiguration : IEntityTypeConfiguration<GoalMetric>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<GoalMetric> builder)
    {
        builder.ToTable("GoalMetrics");

        builder.HasKey(x => x.Id);

        // Index on GoalId for loading goal's scoreboard
        builder.HasIndex(x => x.GoalId);

        // Index on MetricDefinitionId for finding goals using a metric
        builder.HasIndex(x => x.MetricDefinitionId);

        // Unique constraint: same metric can't be in same goal twice
        builder.HasIndex(x => new { x.GoalId, x.MetricDefinitionId }).IsUnique();

        builder.Property(x => x.GoalId)
            .IsRequired();

        builder.Property(x => x.MetricDefinitionId)
            .IsRequired();

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Aggregation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Weight)
            .IsRequired()
            .HasPrecision(3, 2)
            .HasDefaultValue(1.0m);

        builder.Property(x => x.SourceHint)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Baseline)
            .HasPrecision(18, 4);

        builder.Property(x => x.MinimumThreshold)
            .HasPrecision(18, 4);

        // Target as JSON (complex value object)
        builder.Property(x => x.Target)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Target>(v, JsonOptions)!);

        // EvaluationWindow as JSON (complex value object)
        builder.Property(x => x.EvaluationWindow)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<EvaluationWindow>(v, JsonOptions)!);

        // Foreign key to MetricDefinition
        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(x => x.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete metric definition if used in goals

        // Audit fields
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256);
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);
    }
}
