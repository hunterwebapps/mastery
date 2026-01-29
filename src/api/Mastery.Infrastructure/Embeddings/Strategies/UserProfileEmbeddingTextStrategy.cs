using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.UserProfile;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for UserProfile entities.
/// Context depth: Self + CurrentSeason details.
/// </summary>
public sealed class UserProfileEmbeddingTextStrategy : IEmbeddingTextStrategy<UserProfile>
{
    public Task<string?> CompileTextAsync(UserProfile entity, CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Build leading summary: "User with {values count} values, {roles count} roles, in {season name} season"
        var seasonPart = entity.CurrentSeason != null && !entity.CurrentSeason.IsEnded
            ? $", in {entity.CurrentSeason.Label} season"
            : "";
        var activeRolesCount = entity.GetActiveRoles().Count();
        EmbeddingFormatHelper.AppendSummary(sb, "USER-PROFILE",
            $"User with {entity.Values.Count} values, {activeRolesCount} active roles{seasonPart}");

        // Basic info
        sb.AppendLine($"Timezone: {entity.Timezone.IanaId}");

        // Include values with details
        if (entity.Values.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Core Values:");
            foreach (var value in entity.GetValuesOrdered())
            {
                var weightPart = value.Weight.HasValue ? $", Weight: {value.Weight:P0}" : "";
                sb.AppendLine($"  - {value.Label} (Rank {value.Rank}{weightPart})");
                if (!string.IsNullOrWhiteSpace(value.Notes))
                {
                    sb.AppendLine($"    Notes: {value.Notes}");
                }
            }
        }

        // Include roles with details
        var activeRoles = entity.GetActiveRoles().ToList();
        if (activeRoles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Life Roles:");
            foreach (var role in activeRoles.OrderBy(r => r.Rank))
            {
                sb.AppendLine($"  - {role.Label} (Rank {role.Rank}, Season Priority {role.SeasonPriority}/5)");
                if (role.TargetWeeklyMinutes > 0)
                {
                    sb.AppendLine($"    Time allocation: {role.MinWeeklyMinutes}-{role.TargetWeeklyMinutes} min/week");
                }
                if (role.Tags.Count > 0)
                {
                    sb.AppendLine($"    Tags: {string.Join(", ", role.Tags)}");
                }
            }
        }

        // Include current season
        if (entity.CurrentSeason != null && !entity.CurrentSeason.IsEnded)
        {
            sb.AppendLine();
            sb.AppendLine("Current Season:");
            sb.AppendLine($"  Name: {entity.CurrentSeason.Label}");
            sb.AppendLine($"  Type: {FormatSeasonType(entity.CurrentSeason.Type)}");
            sb.AppendLine($"  Intensity: {EmbeddingFormatHelper.FormatIntensity(entity.CurrentSeason.Intensity)}");

            EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "  Success criteria", entity.CurrentSeason.SuccessStatement);

            if (entity.CurrentSeason.NonNegotiables.Count > 0)
            {
                sb.AppendLine($"  Non-negotiables: {string.Join("; ", entity.CurrentSeason.NonNegotiables)}");
            }

            var capacityRange = entity.CurrentSeason.GetCapacityTargetRange();
            sb.AppendLine($"  Target capacity: {capacityRange.MinPercent}-{capacityRange.MaxPercent}%");

            if (entity.CurrentSeason.ExpectedEndDate.HasValue)
            {
                sb.AppendLine($"  Expected end: {EmbeddingFormatHelper.FormatDate(entity.CurrentSeason.ExpectedEndDate.Value)}");
            }
        }

        // Include preferences summary
        if (entity.Preferences != null)
        {
            sb.AppendLine();
            sb.AppendLine("Preferences:");
            sb.AppendLine($"  Coaching style: {FormatCoachingStyle(entity.Preferences.CoachingStyle)}");
            sb.AppendLine($"  Explanation verbosity: {FormatVerbosity(entity.Preferences.ExplanationVerbosity)}");
        }

        // Include constraints summary
        if (entity.Constraints != null)
        {
            sb.AppendLine();
            sb.AppendLine("Constraints:");
            sb.AppendLine($"  Weekday capacity: {entity.Constraints.MaxPlannedMinutesWeekday} min");
            sb.AppendLine($"  Weekend capacity: {entity.Constraints.MaxPlannedMinutesWeekend} min");

            if (entity.Constraints.BlockedTimeWindows.Count > 0)
            {
                sb.AppendLine($"  Blocked windows: {entity.Constraints.BlockedTimeWindows.Count} defined");
            }

            EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "  Health considerations", entity.Constraints.HealthNotes);
        }

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "user", "profile", "values", "roles", "preferences", "constraints",
            "capacity", "coaching", "personalization", "identity");

        return Task.FromResult<string?>(sb.ToString());
    }

    private static string FormatSeasonType(SeasonType type) => type switch
    {
        SeasonType.Sprint => "Sprint (high intensity)",
        SeasonType.Build => "Build (steady progress)",
        SeasonType.Maintain => "Maintain (protect capacity)",
        SeasonType.Recover => "Recover (rest)",
        SeasonType.Transition => "Transition (adaptive)",
        _ => EmbeddingFormatHelper.FormatEnum(type)
    };

    private static string FormatCoachingStyle(CoachingStyle style) => style switch
    {
        CoachingStyle.Direct => "Direct (straight to the point)",
        CoachingStyle.Encouraging => "Encouraging (supportive, motivating)",
        CoachingStyle.Analytical => "Analytical (data-driven reasoning)",
        _ => EmbeddingFormatHelper.FormatEnum(style)
    };

    private static string FormatVerbosity(VerbosityLevel verbosity) => verbosity switch
    {
        VerbosityLevel.Minimal => "Minimal (just the essentials)",
        VerbosityLevel.Medium => "Medium (balanced detail)",
        VerbosityLevel.Detailed => "Detailed (full explanations)",
        _ => EmbeddingFormatHelper.FormatEnum(verbosity)
    };
}
