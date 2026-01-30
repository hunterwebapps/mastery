using System.Reflection;
using Mastery.Domain.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Validates at startup that all domain events have signal classification attributes.
/// Logs warnings for any events missing [SignalClassification] or [NoSignal] attributes.
/// This ensures developers don't accidentally add events without proper classification.
/// </summary>
public sealed class SignalCoverageValidator : IHostedService
{
    private readonly ILogger<SignalCoverageValidator> _logger;

    public SignalCoverageValidator(ILogger<SignalCoverageValidator> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var domainAssembly = typeof(IDomainEvent).Assembly;

        var unclassifiedEvents = new List<string>();

        foreach (var type in domainAssembly.GetTypes())
        {
            // Skip non-class types, abstract types, and types that don't inherit from DomainEvent
            if (!type.IsClass || type.IsAbstract || !type.IsAssignableTo(typeof(IDomainEvent)))
                continue;

            var hasSignalClassification = type.GetCustomAttribute<SignalClassificationAttribute>() != null;
            var hasNoSignal = type.GetCustomAttribute<NoSignalAttribute>() != null;

            if (!hasSignalClassification && !hasNoSignal)
            {
                unclassifiedEvents.Add(type.Name);
            }
        }

        if (unclassifiedEvents.Count > 0)
        {
            _logger.LogWarning(
                "Found {Count} domain events without signal classification attributes: {Events}. " +
                "Add [SignalClassification(...)] or [NoSignal] attribute to each event.",
                unclassifiedEvents.Count,
                string.Join(", ", unclassifiedEvents.OrderBy(e => e)));
        }
        else
        {
            _logger.LogInformation(
                "Signal coverage validation passed: all domain events have classification attributes.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
