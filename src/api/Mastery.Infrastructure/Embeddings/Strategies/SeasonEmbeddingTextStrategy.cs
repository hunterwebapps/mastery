using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Season entities.
/// Context depth: Self + Focus Goals (Title/Status) + Focus Roles (Label/Priority).
/// </summary>
public sealed class SeasonEmbeddingTextStrategy(
    IGoalRepository _goalRepository,
    IUserProfileRepository _userProfileRepository) : IEmbeddingTextStrategy<Season>
{
    public async Task<string?> CompileTextAsync(Season entity, CancellationToken ct)
    {
        // Don't embed ended seasons
        if (entity.IsEnded)
        {
            return null;
        }

        var sb = new StringBuilder();

        // Build leading summary: "{Label} season ({Type}) at {Intensity}/10 intensity, ends {date}"
        var endDatePart = entity.ExpectedEndDate.HasValue
            ? $", ends {EmbeddingFormatHelper.FormatDate(entity.ExpectedEndDate.Value)}"
            : "";
        EmbeddingFormatHelper.AppendSummary(sb, "SEASON",
            $"{entity.Label} season ({FormatSeasonType(entity.Type)}) at {EmbeddingFormatHelper.FormatIntensity(entity.Intensity)} intensity{endDatePart}");

        // Basic info
        sb.AppendLine($"Name: {entity.Label}");
        sb.AppendLine($"Type: {FormatSeasonType(entity.Type)}");
        sb.AppendLine($"Started: {EmbeddingFormatHelper.FormatDate(entity.StartDate)}");
        EmbeddingFormatHelper.AppendDateFieldIfPresent(sb, "Expected end", entity.ExpectedEndDate);

        sb.AppendLine($"Intensity: {EmbeddingFormatHelper.FormatIntensity(entity.Intensity)}");

        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Success criteria", entity.SuccessStatement);

        if (entity.NonNegotiables.Count > 0)
        {
            sb.AppendLine($"Non-negotiables: {string.Join("; ", entity.NonNegotiables)}");
        }

        var capacityRange = entity.GetCapacityTargetRange();
        sb.AppendLine($"Target capacity: {capacityRange.MinPercent}-{capacityRange.MaxPercent}%");

        // Include focus goals with details
        await AppendFocusGoalsAsync(sb, entity, ct);

        // Include focus roles with details
        await AppendFocusRolesAsync(sb, entity, ct);

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "season", "quarter", "focus", "intensity", "capacity",
            "non-negotiables", "priorities", "time period", "planning horizon");

        return sb.ToString();
    }

    private async Task AppendFocusGoalsAsync(StringBuilder sb, Season entity, CancellationToken ct)
    {
        if (entity.FocusGoalIds.Count == 0) return;

        var goals = new List<Goal>();
        foreach (var goalId in entity.FocusGoalIds)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId, ct);
            if (goal != null)
            {
                goals.Add(goal);
            }
        }

        if (goals.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Focus Goals (prioritized this season):");
            foreach (var goal in goals.OrderBy(g => g.Priority))
            {
                sb.AppendLine($"  - {goal.Title} ({EmbeddingFormatHelper.FormatEnum(goal.Status)}, Priority {goal.Priority}/5)");
                if (!string.IsNullOrWhiteSpace(goal.Why))
                {
                    sb.AppendLine($"    Why: {goal.Why}");
                }
                if (goal.Deadline.HasValue)
                {
                    sb.AppendLine($"    Deadline: {EmbeddingFormatHelper.FormatDate(goal.Deadline.Value)}");
                }
            }
        }
    }

    private async Task AppendFocusRolesAsync(StringBuilder sb, Season entity, CancellationToken ct)
    {
        if (entity.FocusRoleIds.Count == 0) return;

        var userProfile = await _userProfileRepository.GetByUserIdAsync(entity.UserId, ct);
        if (userProfile == null) return;

        var focusRoles = userProfile.Roles
            .Where(r => entity.FocusRoleIds.Contains(r.Id))
            .OrderBy(r => r.Rank)
            .ToList();

        if (focusRoles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Focus Roles (prioritized this season):");
            foreach (var role in focusRoles)
            {
                sb.AppendLine($"  - {role.Label} (Priority {role.SeasonPriority}/5, {EmbeddingFormatHelper.FormatEnum(role.Status)})");
                sb.AppendLine($"    Time allocation: {role.MinWeeklyMinutes}-{role.TargetWeeklyMinutes} min/week");
                if (role.Tags.Count > 0)
                {
                    sb.AppendLine($"    Tags: {string.Join(", ", role.Tags)}");
                }
            }
        }
    }

    private static string FormatSeasonType(SeasonType type) => type switch
    {
        SeasonType.Sprint => "Sprint (high intensity, goal-focused)",
        SeasonType.Build => "Build (steady progress, building habits)",
        SeasonType.Maintain => "Maintain (protect capacity and consistency)",
        SeasonType.Recover => "Recover (minimum versions, emphasize rest)",
        SeasonType.Transition => "Transition (flexible, adaptive planning)",
        _ => EmbeddingFormatHelper.FormatEnum(type)
    };
}
