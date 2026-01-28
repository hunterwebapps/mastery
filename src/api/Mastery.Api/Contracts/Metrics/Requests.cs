namespace Mastery.Api.Contracts.Metrics;

/// <summary>
/// Request to create a new metric definition.
/// </summary>
public sealed record CreateMetricDefinitionRequest(
    string Name,
    string? Description = null,
    string DataType = "Number",
    CreateMetricUnitRequest? Unit = null,
    string Direction = "Increase",
    string DefaultCadence = "Daily",
    string DefaultAggregation = "Sum",
    List<string>? Tags = null);

/// <summary>
/// Request to create a metric unit.
/// </summary>
public sealed record CreateMetricUnitRequest(
    string Type,
    string Label);

/// <summary>
/// Request to update a metric definition.
/// </summary>
public sealed record UpdateMetricDefinitionRequest(
    string Name,
    string? Description = null,
    string DataType = "Number",
    CreateMetricUnitRequest? Unit = null,
    string Direction = "Increase",
    string DefaultCadence = "Daily",
    string DefaultAggregation = "Sum",
    bool IsArchived = false,
    List<string>? Tags = null);

/// <summary>
/// Request to record a metric observation.
/// </summary>
public sealed record RecordObservationRequest(
    decimal Value,
    DateOnly? ObservedOn = null,
    string Source = "Manual",
    string? CorrelationId = null,
    string? Note = null);
