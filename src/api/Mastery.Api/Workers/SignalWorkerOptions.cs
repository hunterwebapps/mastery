namespace Mastery.Api.Workers;

/// <summary>
/// Configuration options for signal processing workers.
/// </summary>
public sealed class SignalWorkerOptions
{
    public const string SectionName = "SignalWorkers";

    /// <summary>
    /// Whether signal workers are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Configuration for the urgent signal worker.
    /// </summary>
    public UrgentWorkerOptions Urgent { get; set; } = new();

    /// <summary>
    /// Configuration for the scheduled window worker.
    /// </summary>
    public ScheduledWindowWorkerOptions ScheduledWindow { get; set; } = new();

    /// <summary>
    /// Configuration for the batch signal worker.
    /// </summary>
    public BatchWorkerOptions Batch { get; set; } = new();

    /// <summary>
    /// Timeout per user processing in minutes.
    /// </summary>
    public int TimeoutMinutesPerUser { get; set; } = 2;

    /// <summary>
    /// Maximum retries for failed signal processing.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Lease duration in seconds for acquired signals.
    /// </summary>
    public int LeaseDurationSeconds { get; set; } = 120;
}

/// <summary>
/// Options for the urgent signal worker (P0 signals).
/// </summary>
public sealed class UrgentWorkerOptions
{
    /// <summary>
    /// Whether the urgent worker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Polling interval in seconds (default: 30 seconds).
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum signals to process per cycle.
    /// </summary>
    public int MaxSignalsPerCycle { get; set; } = 50;
}

/// <summary>
/// Options for the scheduled window worker (P1 signals).
/// </summary>
public sealed class ScheduledWindowWorkerOptions
{
    /// <summary>
    /// Whether the window worker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Polling interval in minutes (default: 30 minutes).
    /// </summary>
    public int IntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum users to process per cycle.
    /// </summary>
    public int MaxUsersPerCycle { get; set; } = 100;
}

/// <summary>
/// Options for the batch signal worker (P2/P3 signals).
/// </summary>
public sealed class BatchWorkerOptions
{
    /// <summary>
    /// Whether the batch worker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Polling interval in hours (default: 3 hours).
    /// </summary>
    public int IntervalHours { get; set; } = 3;

    /// <summary>
    /// Maximum users to process per cycle.
    /// </summary>
    public int MaxUsersPerCycle { get; set; } = 200;

    /// <summary>
    /// Maximum signals to process per user.
    /// </summary>
    public int MaxSignalsPerUser { get; set; } = 50;
}
