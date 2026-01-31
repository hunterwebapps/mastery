using System.Text.Json;
using System.Text.Json.Serialization;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Parses and validates LLM-generated recommendation JSON into <see cref="RecommendationCandidate"/> objects.
/// Applies lenient parsing: skips invalid items instead of failing the entire batch.
/// </summary>
internal sealed class LlmResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    private readonly ILogger<LlmResponseParser> _logger;

    public LlmResponseParser(ILogger<LlmResponseParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a Stage 3 generation response into recommendation candidates.
    /// </summary>
    public List<RecommendationCandidate> ParseGenerationResponse(string json, string domain)
    {
        var results = new List<RecommendationCandidate>();

        GenerationResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<GenerationResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse {Domain} generation response JSON", domain);
            return results;
        }

        if (response?.Recommendations is null or { Count: 0 })
        {
            _logger.LogWarning("No recommendations found in {Domain} generation response", domain);
            return results;
        }

        foreach (var item in response.Recommendations)
        {
            var candidate = TryBuildCandidate(item, domain);
            if (candidate is not null)
                results.Add(candidate);
        }

        _logger.LogInformation("Parsed {Count} valid recommendations from {Domain} generation", results.Count, domain);
        return results;
    }

    private RecommendationCandidate? TryBuildCandidate(GeneratedItem item, string domain)
    {
        // Parse recommendation type
        if (!TryParseEnum<RecommendationType>(item.Type, out var recType))
        {
            _logger.LogWarning("Skipping item with invalid type '{Type}' in {Domain}", item.Type, domain);
            return null;
        }

        // Parse target kind
        if (!TryParseEnum<RecommendationTargetKind>(item.TargetKind, out var targetKind))
        {
            _logger.LogWarning("Skipping item with invalid targetKind '{TargetKind}' in {Domain}", item.TargetKind, domain);
            return null;
        }

        // Parse action kind
        if (!TryParseEnum<RecommendationActionKind>(item.ActionKind, out var actionKind))
        {
            _logger.LogWarning("Skipping item with invalid actionKind '{ActionKind}' in {Domain}", item.ActionKind, domain);
            return null;
        }

        // Parse optional target entity ID
        Guid? entityId = null;
        if (!string.IsNullOrWhiteSpace(item.TargetEntityId) && Guid.TryParse(item.TargetEntityId, out var parsedId))
            entityId = parsedId;

        // Title and rationale are required
        if (string.IsNullOrWhiteSpace(item.Title))
        {
            _logger.LogWarning("Skipping item with empty title in {Domain}", domain);
            return null;
        }

        // Normalize score to 0-1 scale
        // LLM may return 0-1 (correct) or 0-100 (legacy) - normalize both to 0-1
        var score = item.Score > 1m
            ? Math.Clamp(item.Score / 100m, 0m, 1m)  // Normalize 0-100 to 0-1
            : Math.Clamp(item.Score, 0m, 1m);

        // Serialize action payload and extract _summary
        string? payloadJson = null;
        string? actionSummary = null;
        if (item.ActionPayload is not null)
        {
            try
            {
                // Extract _summary field if present
                if (item.ActionPayload.Value.TryGetProperty("_summary", out var summaryElement) &&
                    summaryElement.ValueKind == JsonValueKind.String)
                {
                    actionSummary = summaryElement.GetString();
                }

                payloadJson = JsonSerializer.Serialize(item.ActionPayload, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to serialize actionPayload for '{Title}' in {Domain}", item.Title, domain);
            }
        }

        var target = RecommendationTarget.Create(targetKind, entityId, item.TargetEntityTitle);

        return new RecommendationCandidate(
            Type: recType,
            Target: target,
            ActionKind: actionKind,
            Title: item.Title,
            Rationale: item.Rationale ?? "",
            Score: score,
            ActionPayload: payloadJson,
            ActionSummary: actionSummary);
    }

    private static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return Enum.TryParse(value, ignoreCase: true, out result);
    }

    // Internal DTOs for JSON deserialization

    private sealed class GenerationResponse
    {
        [JsonPropertyName("recommendations")]
        public List<GeneratedItem>? Recommendations { get; set; }
    }

    private sealed class GeneratedItem
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("targetKind")]
        public string? TargetKind { get; set; }

        [JsonPropertyName("targetEntityId")]
        public string? TargetEntityId { get; set; }

        [JsonPropertyName("targetEntityTitle")]
        public string? TargetEntityTitle { get; set; }

        [JsonPropertyName("actionKind")]
        public string? ActionKind { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("rationale")]
        public string? Rationale { get; set; }

        [JsonPropertyName("score")]
        public decimal Score { get; set; }

        [JsonPropertyName("actionPayload")]
        public JsonElement? ActionPayload { get; set; }
    }
}
