namespace Mastery.Infrastructure.Messaging;

/// <summary>
/// Configuration options for Azure Service Bus messaging via CAP.
/// Uses queues (Basic tier) instead of topics for cost efficiency.
/// </summary>
public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Whether Service Bus messaging is enabled.
    /// When false, the legacy SQL-based outbox pattern is used.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Azure Service Bus connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Queue name for entity changes pending embedding generation.
    /// </summary>
    public string EmbeddingsQueueName { get; set; } = "embeddings-pending";

    /// <summary>
    /// Queue name for urgent (P0) signals requiring immediate processing.
    /// </summary>
    public string UrgentQueueName { get; set; } = "signals-urgent";

    /// <summary>
    /// Queue name for window-aligned (P1) signals.
    /// </summary>
    public string WindowQueueName { get; set; } = "signals-window";

    /// <summary>
    /// Queue name for batch (P2/P3) signals.
    /// </summary>
    public string BatchQueueName { get; set; } = "signals-batch";

    /// <summary>
    /// Maximum number of retry attempts before moving to DLQ.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Interval in seconds between failed message retries.
    /// </summary>
    public int FailedRetryIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Consumer group name for this application instance.
    /// </summary>
    public string ConsumerGroup { get; set; } = "mastery-api";

    /// <summary>
    /// Whether to enable the CAP dashboard for message monitoring.
    /// </summary>
    public bool EnableDashboard { get; set; } = true;
}
