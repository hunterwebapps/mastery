using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Common.Models;

/// <summary>
/// A candidate recommendation produced by a generator, before ranking and selection.
/// </summary>
public sealed record RecommendationCandidate(
    RecommendationType Type,
    RecommendationTarget Target,
    RecommendationActionKind ActionKind,
    string Title,
    string Rationale,
    decimal Score,
    string? ActionPayload = null,
    string? ActionSummary = null,
    IReadOnlyList<Guid>? ContributingSignalIds = null);
