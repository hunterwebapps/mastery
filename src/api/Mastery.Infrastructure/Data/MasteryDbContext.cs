using System.Reflection;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Common;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Entities.Task;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Identity;
using Mastery.Infrastructure.Messaging;
using Mastery.Infrastructure.Messaging.Events;
using Mastery.Infrastructure.Outbox;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using TaskEntity = Mastery.Domain.Entities.Task.Task;

namespace Mastery.Infrastructure.Data;

public class MasteryDbContext(
    DbContextOptions<MasteryDbContext> options,
    ICurrentUserService _currentUserService,
    IDateTimeProvider _dateTimeProvider,
    IPublisher _publisher,
    IMessageBus _messageBus,
    IOptions<ServiceBusOptions> _serviceBusOptions,
    ILogger<MasteryDbContext> _logger)
    : IdentityDbContext<ApplicationUser>(options), IMasteryDbContext, IUnitOfWork
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalMetric> GoalMetrics => Set<GoalMetric>();
    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();
    public DbSet<MetricObservation> MetricObservations => Set<MetricObservation>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitMetricBinding> HabitMetricBindings => Set<HabitMetricBinding>();
    public DbSet<HabitVariant> HabitVariants => Set<HabitVariant>();
    public DbSet<HabitOccurrence> HabitOccurrences => Set<HabitOccurrence>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskMetricBinding> TaskMetricBindings => Set<TaskMetricBinding>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<ExperimentNote> ExperimentNotes => Set<ExperimentNote>();
    public DbSet<ExperimentResult> ExperimentResults => Set<ExperimentResult>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationTrace> RecommendationTraces => Set<RecommendationTrace>();
    public DbSet<RecommendationRunHistory> RecommendationRunHistory => Set<RecommendationRunHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxEntry> OutboxEntries => Set<OutboxEntry>();
    public DbSet<SignalEntry> SignalEntries => Set<SignalEntry>();
    public DbSet<SignalProcessingHistory> SignalProcessingHistory => Set<SignalProcessingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base first for Identity table configuration
        base.OnModelCreating(modelBuilder);

        // Apply domain entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Capture outbox entries BEFORE save (entity state changes after save)
        var outboxEntries = CaptureOutboxEntries();

        // 2. Capture domain events BEFORE save (they get cleared after dispatch)
        var domainEvents = CaptureDomainEvents();

        // 3. Apply audit fields
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.CreatedAt = _dateTimeProvider.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedBy = _currentUserService.UserId;
                    entry.Entity.ModifiedAt = _dateTimeProvider.UtcNow;
                    break;
            }
        }

        // 4. Add outbox entries to the same transaction (when Service Bus is disabled)
        // When Service Bus is enabled, we still add to outbox for idempotency, but also publish to Service Bus
        if (outboxEntries.Count > 0)
        {
            await OutboxEntries.AddRangeAsync(outboxEntries, cancellationToken);
        }

        // 5. Save changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // 6. Dispatch domain events AFTER successful save
        await DispatchDomainEventsAsync(domainEvents, cancellationToken);

        // 7. Publish to Service Bus after successful save (when enabled)
        if (outboxEntries.Count > 0)
        {
            await PublishToServiceBusAsync(outboxEntries, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Publishes outbox entries to Service Bus as a batch for efficient processing.
    /// Called after successful save when Service Bus is enabled.
    /// </summary>
    private async System.Threading.Tasks.Task PublishToServiceBusAsync(
        List<OutboxEntry> outboxEntries,
        CancellationToken cancellationToken)
    {
        try
        {
            var events = outboxEntries
                .Select(entry => new EntityChangedEvent
                {
                    EntityType = entry.EntityType,
                    EntityId = entry.EntityId,
                    Operation = entry.Operation,
                    UserId = entry.UserId,
                    DomainEventType = entry.DomainEventType,
                    CreatedAt = entry.CreatedAt
                })
                .ToList();

            var batch = new EntityChangedBatchEvent
            {
                Events = events,
            };

            await _messageBus.PublishAsync(
                _serviceBusOptions.Value.EmbeddingsQueueName,
                batch,
                cancellationToken);

            _logger.LogDebug("Published batch of {Count} entity changes to Service Bus", events.Count);
        }
        catch (Exception ex)
        {
            // Log but don't fail - the outbox entries are persisted for SQL-based retry
            _logger.LogError(ex,
                "Failed to publish batch of {Count} entities to Service Bus, will be processed via outbox",
                outboxEntries.Count);
        }
    }

    /// <summary>
    /// Captures all domain events from tracked entities.
    /// </summary>
    private List<(IDomainEvent Event, string? UserId)> CaptureDomainEvents()
    {
        var events = new List<(IDomainEvent, string?)>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity.DomainEvents.Count == 0)
                continue;

            // Try to get UserId from the entity
            string? userId = null;
            if (entry.Entity is OwnedEntity ownedEntity)
            {
                userId = ownedEntity.UserId;
            }

            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                events.Add((domainEvent, userId));
            }

            // Clear events from the entity
            entry.Entity.ClearDomainEvents();
        }

        return events;
    }

    /// <summary>
    /// Dispatches domain events via MediatR for event handlers.
    /// Signal creation is now handled by OutboxProcessingWorker after embeddings are generated.
    /// </summary>
    private async System.Threading.Tasks.Task DispatchDomainEventsAsync(
        List<(IDomainEvent Event, string? UserId)> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
            return;

        foreach (var (domainEvent, userId) in events)
        {
            try
            {
                // Publish via MediatR (for event handlers like metric observation creation)
                await _publisher.Publish(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log but don't fail - domain events are best-effort
                _logger.LogError(ex,
                    "Error dispatching domain event {EventType} for user {UserId}",
                    domainEvent.GetType().Name, userId);
            }
        }
    }

    /// <summary>
    /// Captures entity changes for aggregate roots to be processed by the outbox worker.
    /// Creates one OutboxEntry per domain event (for signal classification).
    /// Skips entities with class-level [EmbeddingIgnore] on Added/Modified.
    /// Skips modifications when only [EmbeddingIgnore] properties changed.
    /// </summary>
    private List<OutboxEntry> CaptureOutboxEntries()
    {
        var entries = new List<OutboxEntry>();
        var now = _dateTimeProvider.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            if (entry.Entity is not AuditableEntity entity)
            {
                continue;
            }

            var entityType = entry.Entity.GetType();
            var isClassIgnored = entityType.GetCustomAttribute<EmbeddingIgnoreAttribute>() is not null;

            // For deletes, create single entry (no domain event)
            if (entry.State == EntityState.Deleted)
            {
                string? userId = (entity as OwnedEntity)?.UserId;
                entries.Add(OutboxEntry.Create(
                    entityType.Name, entity.Id, "Deleted", userId, now, domainEventType: null));
                continue;
            }

            // Skip if not a relevant state
            if (entry.State != EntityState.Added && entry.State != EntityState.Modified) continue;

            // Skip if class is embedding-ignored for Add/Modified
            if (isClassIgnored) continue;
            if (entry.State == EntityState.Modified && !HasEmbeddingRelevantChanges(entry)) continue;

            // Create one OutboxEntry per domain event
            var domainEvents = entry.Entity is BaseEntity baseEntity ? baseEntity.DomainEvents : [];
            string? ownerId = (entity as OwnedEntity)?.UserId;
            var operation = entry.State == EntityState.Added ? "Created" : "Updated";

            if (domainEvents.Count > 0)
            {
                foreach (var domainEvent in domainEvents)
                {
                    entries.Add(OutboxEntry.Create(
                        entityType.Name,
                        entity.Id,
                        operation,
                        ownerId,
                        now,
                        domainEventType: domainEvent.GetType().Name));
                }
            }
            else
            {
                // Entity change without domain event (rare, but handle it)
                entries.Add(OutboxEntry.Create(
                    entityType.Name, entity.Id, operation, ownerId, now, domainEventType: null));
            }
        }

        return entries;
    }

    /// <summary>
    /// Checks if any modified property is NOT marked with [EmbeddingIgnore].
    /// Returns true if at least one embedding-relevant property changed.
    /// </summary>
    private static bool HasEmbeddingRelevantChanges(EntityEntry entry)
    {
        var entityType = entry.Entity.GetType();

        foreach (var property in entry.Properties)
        {
            if (!property.IsModified)
                continue;

            var propertyInfo = entityType.GetProperty(property.Metadata.Name);
            if (propertyInfo?.GetCustomAttribute<EmbeddingIgnoreAttribute>() is null)
                return true;
        }

        return false;
    }
}
