using System.Collections.Frozen;
using System.Reflection;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Common;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Task;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Classifies domain events into signals with appropriate priority and processing windows.
/// Uses reflection at startup to read SignalClassificationAttribute from domain events.
/// </summary>
public sealed class SignalClassifier : ISignalClassifier
{
    private readonly FrozenDictionary<string, SignalMetadata> _classifications;
    private readonly FrozenSet<string> _noSignalEvents;

    public SignalClassifier()
    {
        var classifications = new Dictionary<string, SignalMetadata>();
        var noSignalEvents = new HashSet<string>();

        // Scan all domain event types from the Domain assembly
        var domainAssembly = typeof(IDomainEvent).Assembly;

        foreach (var type in domainAssembly.GetTypes())
        {
            // Skip non-class types, abstract types, and types that don't inherit from DomainEvent
            if (!type.IsClass || type.IsAbstract || !type.IsAssignableTo(typeof(IDomainEvent)))
                continue;

            var signalAttr = type.GetCustomAttribute<SignalClassificationAttribute>();
            var noSignalAttr = type.GetCustomAttribute<NoSignalAttribute>();

            if (signalAttr != null)
            {
                classifications[type.Name] = new SignalMetadata(signalAttr.Priority, signalAttr.WindowType);
            }
            else if (noSignalAttr != null)
            {
                noSignalEvents.Add(type.Name);
            }
            // Events without either attribute will be logged by SignalCoverageValidator
        }

        _classifications = classifications.ToFrozenDictionary();
        _noSignalEvents = noSignalEvents.ToFrozenSet();
    }

    /// <inheritdoc />
    public SignalClassification? ClassifySignal(
        string entityType,
        Guid entityId,
        string? domainEventType,
        string? userId)
    {
        // Skip if no user ID or no event type
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(domainEventType))
            return null;

        // Events explicitly marked as NoSignal should not generate signals
        if (_noSignalEvents.Contains(domainEventType))
            return null;

        // Look up classification from attribute metadata
        if (_classifications.TryGetValue(domainEventType, out var metadata))
        {
            return new SignalClassification(
                metadata.Priority,
                metadata.WindowType,
                domainEventType,
                entityType,
                entityId);
        }

        // Unknown events return null (no signal)
        // SignalCoverageValidator will log warnings for these
        return null;
    }

    /// <summary>
    /// Internal record for caching signal metadata from attributes.
    /// </summary>
    private sealed record SignalMetadata(SignalPriority Priority, ProcessingWindowType WindowType);
}
