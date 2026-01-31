namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events collected from entities before transaction commit.
/// Handles cascading events (events raised by event handlers) in a loop.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all pending domain events from tracked entities.
    /// Continues dispatching until no new events are raised (handles cascading).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DispatchEventsAsync(CancellationToken cancellationToken = default);
}
