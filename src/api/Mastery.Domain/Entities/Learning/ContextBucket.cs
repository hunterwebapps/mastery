namespace Mastery.Domain.Entities.Learning;

/// <summary>
/// Context buckets used for learning what works in different situations.
/// Each bucket represents a distinct context where intervention effectiveness may differ.
/// </summary>
public enum EnergyBucket
{
    /// <summary>Energy level 1-2 (low)</summary>
    Low,
    /// <summary>Energy level 3 (medium)</summary>
    Medium,
    /// <summary>Energy level 4-5 (high)</summary>
    High
}

public enum CapacityBucket
{
    /// <summary>Overloaded (>120% of capacity)</summary>
    Overloaded,
    /// <summary>Full (80-120% of capacity)</summary>
    Full,
    /// <summary>Light (<80% of capacity)</summary>
    Light
}

public enum DayTypeBucket
{
    /// <summary>Monday-Friday</summary>
    Weekday,
    /// <summary>Saturday-Sunday</summary>
    Weekend
}

public enum SeasonIntensityBucket
{
    /// <summary>Season intensity 1-2</summary>
    Low,
    /// <summary>Season intensity 3</summary>
    Medium,
    /// <summary>Season intensity 4-5</summary>
    High
}

/// <summary>
/// Combined context key for playbook entries.
/// </summary>
public sealed record ContextKey(
    EnergyBucket Energy,
    CapacityBucket Capacity,
    DayTypeBucket DayType,
    SeasonIntensityBucket SeasonIntensity)
{
    /// <summary>
    /// Creates a context key from raw values.
    /// </summary>
    public static ContextKey FromValues(
        int energyLevel,
        decimal capacityUtilization,
        DayOfWeek dayOfWeek,
        int seasonIntensity)
    {
        var energy = energyLevel switch
        {
            <= 2 => EnergyBucket.Low,
            3 => EnergyBucket.Medium,
            _ => EnergyBucket.High
        };

        var capacity = capacityUtilization switch
        {
            > 1.2m => CapacityBucket.Overloaded,
            >= 0.8m => CapacityBucket.Full,
            _ => CapacityBucket.Light
        };

        var dayType = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? DayTypeBucket.Weekend
            : DayTypeBucket.Weekday;

        var season = seasonIntensity switch
        {
            <= 2 => SeasonIntensityBucket.Low,
            3 => SeasonIntensityBucket.Medium,
            _ => SeasonIntensityBucket.High
        };

        return new ContextKey(energy, capacity, dayType, season);
    }

    /// <summary>
    /// Serializes to a string key for storage.
    /// </summary>
    public string ToStorageKey() =>
        $"{Energy}:{Capacity}:{DayType}:{SeasonIntensity}";

    /// <summary>
    /// Parses from a storage key.
    /// </summary>
    public static ContextKey? FromStorageKey(string key)
    {
        var parts = key.Split(':');
        if (parts.Length != 4) return null;

        if (!Enum.TryParse<EnergyBucket>(parts[0], out var energy)) return null;
        if (!Enum.TryParse<CapacityBucket>(parts[1], out var capacity)) return null;
        if (!Enum.TryParse<DayTypeBucket>(parts[2], out var dayType)) return null;
        if (!Enum.TryParse<SeasonIntensityBucket>(parts[3], out var season)) return null;

        return new ContextKey(energy, capacity, dayType, season);
    }
}
