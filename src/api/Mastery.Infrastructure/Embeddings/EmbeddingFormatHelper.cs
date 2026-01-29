using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mastery.Infrastructure.Embeddings;

/// <summary>
/// Provides consistent formatting utilities for embedding text strategies.
/// Ensures human-readable output optimized for semantic search.
/// </summary>
public static partial class EmbeddingFormatHelper
{
    /// <summary>
    /// Formats a DateOnly as a human-readable absolute date (e.g., "January 29, 2026").
    /// </summary>
    public static string FormatDate(DateOnly date)
        => date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a DateTime as a human-readable absolute date and time (e.g., "January 29, 2026 at 2:30 PM").
    /// </summary>
    public static string FormatDateTime(DateTime dateTime)
        => dateTime.ToString("MMMM d, yyyy 'at' h:mm tt", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a TimeOnly as human-readable (e.g., "2:30 PM").
    /// </summary>
    public static string FormatTime(TimeOnly time)
        => time.ToString("h:mm tt", CultureInfo.InvariantCulture);

    /// <summary>
    /// Splits a PascalCase or camelCase string into space-separated words.
    /// Example: "NextBestAction" becomes "Next Best Action".
    /// </summary>
    public static string SplitPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return PascalCaseRegex().Replace(value, " $1").Trim();
    }

    /// <summary>
    /// Formats an enum value to a human-readable string by splitting PascalCase.
    /// </summary>
    public static string FormatEnum<T>(T value) where T : Enum
        => SplitPascalCase(value.ToString());

    /// <summary>
    /// Formats a priority value as "X/5 (description)".
    /// </summary>
    public static string FormatPriority(int priority) => priority switch
    {
        5 => "5/5 (Critical)",
        4 => "4/5 (High)",
        3 => "3/5 (Medium)",
        2 => "2/5 (Low)",
        1 => "1/5 (Minimal)",
        _ => $"{priority}/5"
    };

    /// <summary>
    /// Formats an intensity value as "X/10".
    /// </summary>
    public static string FormatIntensity(int intensity)
        => $"{intensity}/10";

    /// <summary>
    /// Formats an energy level as "X/5".
    /// </summary>
    public static string FormatEnergy(int energy)
        => $"{energy}/5";

    /// <summary>
    /// Appends the entity type prefix and leading summary to the StringBuilder.
    /// Format: "[ENTITY-TYPE] summary text"
    /// </summary>
    public static void AppendSummary(StringBuilder sb, string entityType, string summary)
    {
        sb.AppendLine($"[{entityType.ToUpperInvariant()}] {summary}");
        sb.AppendLine();
    }

    /// <summary>
    /// Appends domain keywords to help with semantic search retrieval.
    /// Format: "Keywords: keyword1, keyword2, keyword3"
    /// </summary>
    public static void AppendKeywords(StringBuilder sb, params string[] keywords)
    {
        if (keywords.Length == 0)
            return;

        sb.AppendLine();
        sb.AppendLine($"Keywords: {string.Join(", ", keywords)}");
    }

    /// <summary>
    /// Appends a labeled field if the value is not null or whitespace.
    /// </summary>
    public static void AppendFieldIfNotEmpty(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{label}: {value}");
        }
    }

    /// <summary>
    /// Appends a labeled field with the formatted date if the date has a value.
    /// </summary>
    public static void AppendDateFieldIfPresent(StringBuilder sb, string label, DateOnly? date)
    {
        if (date.HasValue)
        {
            sb.AppendLine($"{label}: {FormatDate(date.Value)}");
        }
    }

    /// <summary>
    /// Appends a labeled field with the formatted DateTime if it has a value.
    /// </summary>
    public static void AppendDateTimeFieldIfPresent(StringBuilder sb, string label, DateTime? dateTime)
    {
        if (dateTime.HasValue)
        {
            sb.AppendLine($"{label}: {FormatDateTime(dateTime.Value)}");
        }
    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex PascalCaseRegex();
}
