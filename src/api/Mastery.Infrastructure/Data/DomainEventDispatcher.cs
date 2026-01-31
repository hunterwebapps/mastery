using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Data;

/// <summary>
/// Dispatches domain events from tracked entities before transaction commit.
/// Handles cascading events by looping until no new events are raised.
/// </summary>
public sealed class DomainEventDispatcher(
    MasteryDbContext _dbContext,
    IPublisher _publisher,
    ILogger<DomainEventDispatcher> _logger)
    : IDomainEventDispatcher
{
    private const int MaxDispatchIterations = 10;

    public async Task DispatchEventsAsync(CancellationToken cancellationToken = default)
    {
        var iteration = 0;

        while (iteration < MaxDispatchIterations)
        {
            var domainEvents = CollectDomainEvents();

            if (domainEvents.Count == 0)
                break;

            _logger.LogDebug(
                "Dispatching {Count} domain events (iteration {Iteration})",
                domainEvents.Count,
                iteration + 1);

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }

            iteration++;
        }

        if (iteration == MaxDispatchIterations)
        {
            _logger.LogError(
                "Domain event dispatch reached maximum iterations ({MaxIterations}). " +
                "This may indicate an infinite event loop.",
                MaxDispatchIterations);
        }
    }

    private List<INotification> CollectDomainEvents()
    {
        var domainEvents = new List<INotification>();

        foreach (var entry in _dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity.DomainEvents.Count > 0)
            {
                domainEvents.AddRange(entry.Entity.DomainEvents);
                entry.Entity.ClearDomainEvents();
            }
        }

        return domainEvents;
    }
}
