using System.Text;
using Mastery.Application.Common.Models;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Formats RAG context for inclusion in LLM prompts.
/// Provides stage-specific formatting with outcome badges and temporal context.
/// </summary>
internal static class RagContextFormatter
{
    /// <summary>
    /// Formats RAG context for Assessment stage.
    /// Focus: patterns, trends, energy trajectories.
    /// Narrative style for situational understanding.
    /// </summary>
    public static string? FormatForAssessment(RagContext? context, DateOnly today)
    {
        if (context is null || context.Items.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("## Historical Patterns (from similar situations)");
        sb.AppendLine("Use these to identify recurring themes and inform your assessment:");
        sb.AppendLine();

        foreach (var item in context.Items)
        {
            var recency = FormatRecency(item, today);
            var relevance = (int)(item.SimilarityScore * 100);

            sb.AppendLine($"### {item.EntityType}: {item.Title}");
            sb.AppendLine($"({recency} | {relevance}% relevance)");

            if (!string.IsNullOrEmpty(item.EmbeddingText))
            {
                // Extract key narrative from embedding text
                var narrative = ExtractNarrative(item.EmbeddingText);
                sb.AppendLine(narrative);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats RAG context for Strategy stage.
    /// Focus: what interventions worked/failed, why dismissed.
    /// Decision-support style with outcome badges.
    /// </summary>
    public static string? FormatForStrategy(RagContext? context, DateOnly today)
    {
        if (context is null || context.Items.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("## Past Intervention Outcomes (learn from these)");
        sb.AppendLine("IMPORTANT: Avoid repeating recently dismissed interventions. Build on what worked.");
        sb.AppendLine();

        foreach (var item in context.Items)
        {
            var recency = FormatRecency(item, today);
            var badge = FormatOutcomeBadge(item);
            var relevance = (int)(item.SimilarityScore * 100);

            sb.AppendLine($"{badge} [{item.EntityType}] {item.Title}");
            sb.AppendLine($"   ({recency} | {relevance}% relevance)");

            // Include dismiss reason if available - critical for learning
            var dismissReason = ExtractDismissReason(item.EmbeddingText);
            if (!string.IsNullOrEmpty(dismissReason))
            {
                sb.AppendLine($"   Dismiss reason: {dismissReason}");
            }

            // Include outcome summary if experiment
            if (item.EntityType == "Experiment")
            {
                var outcome = ExtractExperimentOutcome(item.EmbeddingText);
                if (!string.IsNullOrEmpty(outcome))
                {
                    sb.AppendLine($"   Outcome: {outcome}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats RAG context for Generation stage.
    /// Focus: similar past recommendations, outcomes.
    /// Domain-filtered with actionPayload pattern hints.
    /// </summary>
    public static string? FormatForGeneration(RagContext? context, string domain, DateOnly today)
    {
        if (context is null || context.Items.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine($"## Related {domain} History (inform your recommendations)");
        sb.AppendLine("Use successful past recommendations as templates. Avoid patterns that led to dismissals.");
        sb.AppendLine();

        foreach (var item in context.Items)
        {
            var recency = FormatRecency(item, today);
            var badge = FormatOutcomeBadge(item);
            var relevance = (int)(item.SimilarityScore * 100);

            sb.AppendLine($"{badge} {item.Title}");
            sb.AppendLine($"   Type: {item.EntityType} | {recency} | {relevance}% relevance");

            // Include key context from embedding
            if (!string.IsNullOrEmpty(item.EmbeddingText))
            {
                var keyContext = ExtractKeyContext(item.EmbeddingText, domain);
                if (!string.IsNullOrEmpty(keyContext))
                {
                    sb.AppendLine($"   {keyContext}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generic format for backward compatibility.
    /// </summary>
    public static string? FormatForPrompt(RagContext? context, string sectionTitle = "Historical Context")
    {
        if (context is null || context.Items.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine($"## {sectionTitle}");
        sb.AppendLine("The following items were retrieved based on semantic similarity to your current situation:");
        sb.AppendLine();

        foreach (var item in context.Items)
        {
            var badge = FormatOutcomeBadge(item);
            var relevance = (int)(item.SimilarityScore * 100);

            sb.AppendLine($"### {badge} [{item.EntityType}] {item.Title}");
            sb.AppendLine($"Relevance: {relevance}%");

            if (!string.IsNullOrEmpty(item.EmbeddingText))
            {
                sb.AppendLine($"Context: {item.EmbeddingText}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats RAG context as a concise list for system prompts.
    /// </summary>
    public static string? FormatConcise(RagContext? context, string prefix = "Related history")
    {
        if (context is null || context.Items.Count == 0)
            return null;

        var items = context.Items
            .Select(i => $"{FormatOutcomeBadge(i)} {i.EntityType}: \"{i.Title}\" ({(int)(i.SimilarityScore * 100)}% match)")
            .ToList();

        return $"{prefix}: {string.Join("; ", items)}";
    }

    /// <summary>
    /// Appends RAG context to a StringBuilder if context is available.
    /// Uses generic format.
    /// </summary>
    public static void AppendIfPresent(StringBuilder sb, RagContext? context, string sectionTitle = "Historical Context")
    {
        var formatted = FormatForPrompt(context, sectionTitle);
        if (formatted is not null)
        {
            sb.AppendLine(formatted);
        }
    }

    /// <summary>
    /// Appends Assessment-specific RAG context to a StringBuilder.
    /// </summary>
    public static void AppendForAssessment(StringBuilder sb, RagContext? context, DateOnly today)
    {
        var formatted = FormatForAssessment(context, today);
        if (formatted is not null)
        {
            sb.AppendLine(formatted);
        }
    }

    /// <summary>
    /// Appends Strategy-specific RAG context to a StringBuilder.
    /// </summary>
    public static void AppendForStrategy(StringBuilder sb, RagContext? context, DateOnly today)
    {
        var formatted = FormatForStrategy(context, today);
        if (formatted is not null)
        {
            sb.AppendLine(formatted);
        }
    }

    /// <summary>
    /// Appends Generation-specific RAG context to a StringBuilder.
    /// </summary>
    public static void AppendForGeneration(StringBuilder sb, RagContext? context, string domain, DateOnly today)
    {
        var formatted = FormatForGeneration(context, domain, today);
        if (formatted is not null)
        {
            sb.AppendLine(formatted);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private static string FormatOutcomeBadge(RagContextItem item)
    {
        // Check status for recommendation outcomes
        return item.Status?.ToLowerInvariant() switch
        {
            "accepted" => "[ACCEPTED]",
            "executed" => "[EXECUTED]",
            "dismissed" => "[DISMISSED]",
            "expired" => "[EXPIRED]",
            "pending" => "[PENDING]",
            "completed" => "[COMPLETED]",
            "active" => "[ACTIVE]",
            "abandoned" => "[ABANDONED]",
            _ when !string.IsNullOrEmpty(item.Status) => $"[{item.Status.ToUpperInvariant()}]",
            _ => ""
        };
    }

    private static string FormatRecency(RagContextItem item, DateOnly today)
    {
        // Try to extract date from embedding text
        // Look for patterns like "Started: 2024-01-15" or "Date: 2024-01-15"
        var dateMatch = System.Text.RegularExpressions.Regex.Match(
            item.EmbeddingText ?? "",
            @"(?:Started|Date|Created|Completed):\s*(\d{4}-\d{2}-\d{2})");

        if (dateMatch.Success && DateOnly.TryParse(dateMatch.Groups[1].Value, out var date))
        {
            var days = today.DayNumber - date.DayNumber;
            return days switch
            {
                0 => "today",
                1 => "yesterday",
                < 7 => $"{days} days ago",
                < 14 => "last week",
                < 30 => $"{days / 7} weeks ago",
                _ => $"{days / 30} months ago"
            };
        }

        return "recent";
    }

    private static string ExtractNarrative(string embeddingText)
    {
        // Extract first 2-3 meaningful lines for narrative context
        var lines = embeddingText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.StartsWith("Keywords:", StringComparison.OrdinalIgnoreCase))
            .Take(3);
        return string.Join(" ", lines).Trim();
    }

    private static string? ExtractDismissReason(string? embeddingText)
    {
        if (string.IsNullOrEmpty(embeddingText))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            embeddingText,
            @"Dismiss reason:\s*(.+?)(?:\n|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractExperimentOutcome(string? embeddingText)
    {
        if (string.IsNullOrEmpty(embeddingText))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            embeddingText,
            @"Outcome:\s*(.+?)(?:\n|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractKeyContext(string? embeddingText, string domain)
    {
        if (string.IsNullOrEmpty(embeddingText))
            return null;

        // Extract domain-relevant information
        return domain switch
        {
            "Task" => ExtractField(embeddingText, "Suggested action") ??
                      ExtractField(embeddingText, "Action summary"),
            "Habit" => ExtractField(embeddingText, "Rationale") ??
                       ExtractField(embeddingText, "Target"),
            "Experiment" => ExtractField(embeddingText, "Hypothesis") ??
                           ExtractField(embeddingText, "Result"),
            "Project" => ExtractField(embeddingText, "Suggested action") ??
                        ExtractField(embeddingText, "Rationale"),
            _ => ExtractField(embeddingText, "Rationale")
        };
    }

    private static string? ExtractField(string text, string fieldName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            $@"{fieldName}:\s*(.+?)(?:\n|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? $"{fieldName}: {match.Groups[1].Value.Trim()}" : null;
    }
}
