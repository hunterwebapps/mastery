using System.Text.Json.Serialization;
using Mastery.Domain.Common;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Evidence backing a diagnostic signal.
/// </summary>
public sealed class SignalEvidence : ValueObject
{
    public string Metric { get; }
    public decimal CurrentValue { get; }
    public decimal? ThresholdValue { get; }
    public string? Detail { get; }

    private SignalEvidence()
    {
        Metric = string.Empty;
        CurrentValue = 0;
    }

    [JsonConstructor]
    public SignalEvidence(string metric, decimal currentValue, decimal? thresholdValue, string? detail)
    {
        Metric = metric;
        CurrentValue = currentValue;
        ThresholdValue = thresholdValue;
        Detail = detail;
    }

    public static SignalEvidence Create(
        string metric,
        decimal currentValue,
        decimal? thresholdValue = null,
        string? detail = null)
    {
        return new SignalEvidence(metric, currentValue, thresholdValue, detail);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Metric;
        yield return CurrentValue;
        yield return ThresholdValue;
        yield return Detail;
    }
}
