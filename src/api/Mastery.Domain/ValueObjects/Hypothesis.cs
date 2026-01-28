using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents a testable hypothesis for an experiment.
/// Follows the structure: "If I [change], then [expected outcome] because [rationale]."
/// </summary>
public sealed class Hypothesis : ValueObject
{
    /// <summary>
    /// The change being made (the independent variable).
    /// </summary>
    public string Change { get; } = null!;

    /// <summary>
    /// The expected outcome if the hypothesis is correct.
    /// </summary>
    public string ExpectedOutcome { get; } = null!;

    /// <summary>
    /// The reasoning behind the hypothesis (optional).
    /// </summary>
    public string? Rationale { get; }

    private Hypothesis()
    {
        Change = string.Empty;
        ExpectedOutcome = string.Empty;
    }

    [JsonConstructor]
    public Hypothesis(string change, string expectedOutcome, string? rationale = null)
    {
        Change = change;
        ExpectedOutcome = expectedOutcome;
        Rationale = rationale;
    }

    /// <summary>
    /// Creates a new Hypothesis with validation.
    /// </summary>
    public static Hypothesis Create(string change, string expectedOutcome, string? rationale = null)
    {
        if (string.IsNullOrWhiteSpace(change))
            throw new DomainException("Hypothesis change cannot be empty.");

        if (change.Length > 500)
            throw new DomainException("Hypothesis change cannot exceed 500 characters.");

        if (string.IsNullOrWhiteSpace(expectedOutcome))
            throw new DomainException("Hypothesis expected outcome cannot be empty.");

        if (expectedOutcome.Length > 500)
            throw new DomainException("Hypothesis expected outcome cannot exceed 500 characters.");

        if (rationale != null && rationale.Length > 1000)
            throw new DomainException("Hypothesis rationale cannot exceed 1000 characters.");

        return new Hypothesis(change, expectedOutcome, rationale);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Change;
        yield return ExpectedOutcome;
        yield return Rationale;
    }

    public override string ToString() =>
        $"If I {Change}, then {ExpectedOutcome}{(Rationale != null ? $" because {Rationale}" : "")}.";
}
