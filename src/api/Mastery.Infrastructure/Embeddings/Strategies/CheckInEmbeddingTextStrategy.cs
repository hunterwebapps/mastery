using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Enums;
using Task = Mastery.Domain.Entities.Task.Task;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for CheckIn entities.
/// Context depth: Self + Top1 entity (Task/Habit/Project) when referenced.
/// </summary>
public sealed class CheckInEmbeddingTextStrategy(IEntityResolver _entityResolver) : IEmbeddingTextStrategy<CheckIn>
{
    public async Task<string?> CompileTextAsync(CheckIn entity, CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Build leading summary
        var checkInType = entity.Type == CheckInType.Morning ? "Morning" : "Evening";
        var energyLevel = entity.Type == CheckInType.Morning
            ? entity.EnergyLevel
            : entity.EnergyLevelPm;
        var energySummary = energyLevel.HasValue
            ? $" with {EmbeddingFormatHelper.FormatEnergy(energyLevel.Value)} energy"
            : "";

        EmbeddingFormatHelper.AppendSummary(sb, "CHECK-IN",
            $"{checkInType} check-in on {EmbeddingFormatHelper.FormatDate(entity.CheckInDate)}{energySummary}");

        // Basic info
        sb.AppendLine($"Date: {EmbeddingFormatHelper.FormatDate(entity.CheckInDate)}");
        sb.AppendLine($"Type: {checkInType}");
        sb.AppendLine($"Status: {EmbeddingFormatHelper.FormatEnum(entity.Status)}");

        if (entity.CompletedAt.HasValue)
        {
            sb.AppendLine($"Completed at: {EmbeddingFormatHelper.FormatTime(TimeOnly.FromDateTime(entity.CompletedAt.Value))}");
        }

        if (entity.Type == CheckInType.Morning)
        {
            await AppendMorningDetailsAsync(sb, entity, ct);
        }
        else
        {
            AppendEveningDetails(sb, entity);
        }

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "check-in", "energy", "stress", "blocker", "intention", "reflection",
            "daily loop", "morning routine", "evening review", "adherence", "friction");

        return sb.ToString();
    }

    private async System.Threading.Tasks.Task AppendMorningDetailsAsync(StringBuilder sb, CheckIn entity, CancellationToken ct)
    {
        if (entity.EnergyLevel.HasValue)
        {
            sb.AppendLine($"Morning energy: {EmbeddingFormatHelper.FormatEnergy(entity.EnergyLevel.Value)}");
        }

        if (entity.SelectedMode.HasValue)
        {
            sb.AppendLine($"Selected mode: {EmbeddingFormatHelper.FormatEnum(entity.SelectedMode.Value)}");
        }

        if (entity.Top1Type.HasValue)
        {
            var top1Description = await ResolveTop1DescriptionAsync(
                entity.Top1Type.Value,
                entity.Top1EntityId,
                entity.Top1FreeText,
                ct);
            sb.AppendLine($"Top priority ({EmbeddingFormatHelper.FormatEnum(entity.Top1Type.Value)}): {top1Description}");
        }

        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Intention", entity.Intention);
    }

    private static void AppendEveningDetails(StringBuilder sb, CheckIn entity)
    {
        if (entity.EnergyLevelPm.HasValue)
        {
            sb.AppendLine($"Evening energy: {EmbeddingFormatHelper.FormatEnergy(entity.EnergyLevelPm.Value)}");
        }

        if (entity.StressLevel.HasValue)
        {
            sb.AppendLine($"Stress level: {entity.StressLevel}/5");
        }

        if (entity.Top1Completed.HasValue)
        {
            sb.AppendLine($"Top priority completed: {(entity.Top1Completed.Value ? "Yes" : "No")}");
        }

        if (entity.BlockerCategory.HasValue)
        {
            sb.AppendLine($"Blocker category: {EmbeddingFormatHelper.FormatEnum(entity.BlockerCategory.Value)}");
            EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Blocker details", entity.BlockerNote);
        }

        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Reflection", entity.Reflection);
    }

    private async Task<string> ResolveTop1DescriptionAsync(
        Top1Type top1Type,
        Guid? entityId,
        string? freeText,
        CancellationToken ct)
    {
        if (top1Type == Top1Type.FreeText)
        {
            return freeText ?? "unspecified";
        }

        if (!entityId.HasValue)
        {
            return "unspecified";
        }

        var entityTypeName = top1Type switch
        {
            Top1Type.Task => nameof(Task),
            Top1Type.Habit => nameof(Habit),
            Top1Type.Project => nameof(Project),
            _ => null
        };

        if (entityTypeName == null)
        {
            return "unspecified";
        }

        var entity = await _entityResolver.ResolveAsync(entityTypeName, entityId.Value, ct);

        return entity switch
        {
            Task task => task.Title,
            Habit habit => habit.Title,
            Project project => project.Title,
            _ => "unspecified"
        };
    }
}
