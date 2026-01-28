using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Identifies the entity a recommendation targets.
/// </summary>
public sealed class RecommendationTarget : ValueObject
{
    public RecommendationTargetKind Kind { get; }
    public Guid? EntityId { get; }
    public string? EntityTitle { get; }

    private RecommendationTarget()
    {
        Kind = RecommendationTargetKind.UserProfile;
    }

    [JsonConstructor]
    public RecommendationTarget(RecommendationTargetKind kind, Guid? entityId, string? entityTitle)
    {
        Kind = kind;
        EntityId = entityId;
        EntityTitle = entityTitle;
    }

    public static RecommendationTarget Create(
        RecommendationTargetKind kind,
        Guid? entityId = null,
        string? entityTitle = null)
    {
        return new RecommendationTarget(kind, entityId, entityTitle);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return EntityId;
        yield return EntityTitle;
    }
}
