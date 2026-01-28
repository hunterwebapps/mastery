using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Metrics.Commands.CreateMetricDefinition;

namespace Mastery.Application.Features.Metrics.Commands.UpdateMetricDefinition;

/// <summary>
/// Updates a metric definition in the user's metric library.
/// </summary>
public sealed record UpdateMetricDefinitionCommand(
    Guid Id,
    string Name,
    string? Description = null,
    string DataType = "Number",
    CreateMetricUnitInput? Unit = null,
    string Direction = "Increase",
    string DefaultCadence = "Daily",
    string DefaultAggregation = "Sum",
    bool IsArchived = false,
    List<string>? Tags = null) : ICommand;
