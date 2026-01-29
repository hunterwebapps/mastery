using System.Text.Json;
using Mastery.Domain.Common;

namespace Mastery.Domain.Entities.Recommendation;

/// <summary>
/// Tracks each execution cycle of the background recommendation worker.
/// </summary>
[EmbeddingIgnore]
public sealed class RecommendationRunHistory : BaseEntity, IAggregateRoot
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int UsersEvaluated { get; private set; }
    public int UsersProcessed { get; private set; }
    public int RecommendationsGenerated { get; private set; }
    public int Errors { get; private set; }
    public string? ErrorDetails { get; private set; }
    public string Status { get; private set; } = null!;

    private RecommendationRunHistory() { }

    public static RecommendationRunHistory Start(DateTime startedAt, int usersEvaluated)
    {
        return new RecommendationRunHistory
        {
            StartedAt = startedAt,
            UsersEvaluated = usersEvaluated,
            UsersProcessed = 0,
            RecommendationsGenerated = 0,
            Errors = 0,
            Status = "Running"
        };
    }

    public void RecordUserProcessed(int recommendationCount)
    {
        UsersProcessed++;
        RecommendationsGenerated += recommendationCount;
    }

    public void RecordError(string userId, string errorMessage)
    {
        Errors++;

        var errors = string.IsNullOrEmpty(ErrorDetails)
            ? new List<object>()
            : JsonSerializer.Deserialize<List<object>>(ErrorDetails, JsonOptions) ?? [];

        errors.Add(new { userId, error = Truncate(errorMessage, 500) });
        ErrorDetails = JsonSerializer.Serialize(errors, JsonOptions);
    }

    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
        Status = Errors > 0 && UsersProcessed == 0 ? "Failed" : "Completed";
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
