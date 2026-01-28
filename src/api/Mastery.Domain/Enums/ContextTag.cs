namespace Mastery.Domain.Enums;

/// <summary>
/// Context tags for tasks to enable context-aware Next Best Action ranking.
/// Tasks are filtered by available context during NBA generation.
/// </summary>
public enum ContextTag
{
    /// <summary>
    /// Requires a computer/laptop.
    /// </summary>
    Computer,

    /// <summary>
    /// Can be done from phone.
    /// </summary>
    Phone,

    /// <summary>
    /// Requires going out (shopping, errands).
    /// </summary>
    Errands,

    /// <summary>
    /// Best done at home.
    /// </summary>
    Home,

    /// <summary>
    /// Best done at office/workplace.
    /// </summary>
    Office,

    /// <summary>
    /// Requires deep focus / distraction-free environment.
    /// </summary>
    DeepWork,

    /// <summary>
    /// Suitable for low-energy states.
    /// </summary>
    LowEnergy,

    /// <summary>
    /// Can be done anywhere.
    /// </summary>
    Anywhere
}
