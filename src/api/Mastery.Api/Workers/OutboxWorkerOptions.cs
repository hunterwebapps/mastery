namespace Mastery.Api.Workers;

/// <summary>
/// Configuration options for the outbox processing background worker.
/// </summary>
public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";

    /// <summary>
    /// Whether the outbox worker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Polling interval in milliseconds.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Maximum number of entries to process per batch.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Lease duration in minutes before an entry can be reprocessed.
    /// </summary>
    public int LeaseMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum number of retry attempts before marking an entry as failed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
