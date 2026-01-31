using System.Text;
using Mastery.Application.Common.Models;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Formats RAG context for inclusion in LLM prompts.
/// </summary>
internal static class RagContextFormatter
{
    /// <summary>
    /// Formats RAG context as a markdown section for inclusion in prompts.
    /// Returns null if no context is available.
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
            var statusStr = !string.IsNullOrEmpty(item.Status) ? $" | Status: {item.Status}" : "";
            var relevance = (int)(item.SimilarityScore * 100);

            sb.AppendLine($"### [{item.EntityType}] {item.Title}");
            sb.AppendLine($"Relevance: {relevance}%{statusStr}");

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
            .Select(i => $"{i.EntityType}: \"{i.Title}\" ({(int)(i.SimilarityScore * 100)}% match)")
            .ToList();

        return $"{prefix}: {string.Join("; ", items)}";
    }

    /// <summary>
    /// Appends RAG context to a StringBuilder if context is available.
    /// </summary>
    public static void AppendIfPresent(StringBuilder sb, RagContext? context, string sectionTitle = "Historical Context")
    {
        var formatted = FormatForPrompt(context, sectionTitle);
        if (formatted is not null)
        {
            sb.AppendLine(formatted);
        }
    }
}
