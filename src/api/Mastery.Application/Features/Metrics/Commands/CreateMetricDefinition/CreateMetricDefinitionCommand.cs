using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Metrics.Commands.CreateMetricDefinition;

/// <summary>
/// Creates a new metric definition in the user's metric library.
/// </summary>
public sealed record CreateMetricDefinitionCommand(
    string Name,
    string? Description = null,
    string DataType = "Number",
    CreateMetricUnitInput? Unit = null,
    string Direction = "Increase",
    string DefaultCadence = "Daily",
    string DefaultAggregation = "Sum",
    List<string>? Tags = null) : ICommand<Guid>;

/// <summary>
/// Input for creating a metric unit.
/// </summary>
public sealed record CreateMetricUnitInput(
    string Type,
    string Label);
