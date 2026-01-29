using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for MetricDefinition entities.
/// Context depth: Self-contained.
/// </summary>
public sealed class MetricDefinitionEmbeddingTextStrategy : IEmbeddingTextStrategy<MetricDefinition>
{
    public Task<string?> CompileTextAsync(MetricDefinition entity, CancellationToken ct)
    {
        // Don't embed archived metrics
        if (entity.IsArchived)
        {
            return Task.FromResult<string?>(null);
        }

        var sb = new StringBuilder();

        // Build leading summary: "{Name}: {Description} ({Direction} is better)"
        var directionText = FormatDirectionShort(entity.Direction);
        var descriptionPart = !string.IsNullOrWhiteSpace(entity.Description)
            ? $": {entity.Description}"
            : "";
        EmbeddingFormatHelper.AppendSummary(sb, "METRIC",
            $"{entity.Name}{descriptionPart} ({directionText})");

        // Basic metric information
        sb.AppendLine($"Name: {entity.Name}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Description", entity.Description);

        sb.AppendLine($"Data type: {FormatDataType(entity.DataType)}");
        sb.AppendLine($"Unit: {entity.Unit.DisplayLabel} ({FormatUnitType(entity.Unit.UnitType)})");
        sb.AppendLine($"Direction: {FormatDirection(entity.Direction)}");
        sb.AppendLine($"Default cadence: {FormatCadence(entity.DefaultCadence)}");
        sb.AppendLine($"Default aggregation: {FormatAggregation(entity.DefaultAggregation)}");

        if (entity.Tags.Count > 0)
        {
            sb.AppendLine($"Tags: {string.Join(", ", entity.Tags)}");
        }

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "metric", "measurement", "KPI", "indicator", "lead", "lag",
            "tracking", "data", "progress", "quantitative");

        return Task.FromResult<string?>(sb.ToString());
    }

    private static string FormatDirectionShort(MetricDirection direction) => direction switch
    {
        MetricDirection.Increase => "higher is better",
        MetricDirection.Decrease => "lower is better",
        MetricDirection.Maintain => "maintain within range",
        _ => EmbeddingFormatHelper.FormatEnum(direction)
    };

    private static string FormatDirection(MetricDirection direction) => direction switch
    {
        MetricDirection.Increase => "Higher is Better",
        MetricDirection.Decrease => "Lower is Better",
        MetricDirection.Maintain => "Maintain (stay within range)",
        _ => EmbeddingFormatHelper.FormatEnum(direction)
    };

    private static string FormatDataType(MetricDataType dataType) => dataType switch
    {
        MetricDataType.Number => "Number",
        MetricDataType.Boolean => "Boolean (yes/no)",
        MetricDataType.Duration => "Duration (time in minutes)",
        MetricDataType.Percentage => "Percentage",
        MetricDataType.Count => "Count",
        MetricDataType.Rating => "Rating (1-5 scale)",
        _ => EmbeddingFormatHelper.FormatEnum(dataType)
    };

    private static string FormatUnitType(string unitType) => unitType switch
    {
        "count" => "count",
        "duration" => "time/duration",
        "currency" => "currency",
        "distance" => "distance",
        "weight" => "weight",
        "percentage" => "percentage",
        "rating" => "rating",
        "boolean" => "boolean",
        "energy" => "energy",
        "none" => "unitless",
        _ => unitType
    };

    private static string FormatCadence(WindowType cadence) => cadence switch
    {
        WindowType.Daily => "Daily",
        WindowType.Weekly => "Weekly",
        WindowType.Monthly => "Monthly",
        _ => EmbeddingFormatHelper.FormatEnum(cadence)
    };

    private static string FormatAggregation(MetricAggregation aggregation) => aggregation switch
    {
        MetricAggregation.Sum => "Sum (total)",
        MetricAggregation.Average => "Average (mean)",
        MetricAggregation.Min => "Minimum",
        MetricAggregation.Max => "Maximum",
        MetricAggregation.Latest => "Latest value",
        MetricAggregation.Count => "Count of observations",
        _ => EmbeddingFormatHelper.FormatEnum(aggregation)
    };
}
