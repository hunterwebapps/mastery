using System.Diagnostics;
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
    IDomainEventDispatcher _domainEventDispatcher,
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
        // 1. Dispatch domain events BEFORE commit (handlers run within the same transaction)
        // This allows cascading events and ensures all changes are committed together
        await _domainEventDispatcher.DispatchEventsAsync(
            () => ChangeTracker.Entries<BaseEntity>().Select(e => e.Entity),
            cancellationToken);

        // 2. Collect Service Bus snapshots from all entities with domain events
        var entityChangeSnapshots = CollectServiceBusSnapshots();

        // 3. Apply audit fields
        ApplyAuditFields();

        // 4. Single commit - all changes from command handler AND event handlers
        var result = await base.SaveChangesAsync(cancellationToken);

        // 5. Publish to Service Bus after successful save (for embedding generation)
        await PublishToServiceBusAsync(entityChangeSnapshots, cancellationToken);

        return result;
    }

    private List<EntityChangeSnapshot> CollectServiceBusSnapshots()
    {
        var snapshots = new List<EntityChangeSnapshot>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (!HasEmbeddingRelevantChanges(entry))
            {
                continue;
            }

            if (entry.Entity is OwnedEntity ownedEntity &&
                (entry.State == EntityState.Added ||
                 entry.State == EntityState.Modified ||
                 entry.State == EntityState.Deleted))
            {
                snapshots.Add(new EntityChangeSnapshot(
                    EntityType: entry.Entity.GetType().Name,
                    EntityId: entry.Entity.Id,
                    Operation: entry.State switch
                    {
                        EntityState.Added => "Created",
                        EntityState.Modified => "Updated",
                        EntityState.Deleted => "Deleted",
                        _ => "Unknown"
                    },
                    UserId: ownedEntity.UserId,
                    DomainEventTypes: entry.Entity.DomainEvents
                        .Select(e => e.GetType().Name)
                        .ToArray()
                ));
            }
        }

        return snapshots;
    }

    private void ApplyAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity is AuditableEntity auditableEntry)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditableEntry.CreatedBy = _currentUserService.UserId;
                        auditableEntry.CreatedAt = _dateTimeProvider.UtcNow;
                        break;

                    case EntityState.Modified:
                        auditableEntry.ModifiedBy = _currentUserService.UserId;
                        auditableEntry.ModifiedAt = _dateTimeProvider.UtcNow;
                        break;
                }
            }
        }
    }

    private sealed record EntityChangeSnapshot(
        string EntityType,
        Guid EntityId,
        string Operation,
        string UserId,
        string[] DomainEventTypes);

    /// <summary>
    /// Publishes entity change events to Service Bus as batched events (per user).
    /// Called after successful save for embedding generation pipeline.
    /// </summary>
    private async System.Threading.Tasks.Task PublishToServiceBusAsync(
        List<EntityChangeSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        if (snapshots.Count == 0)
            return;

        // Group by user for batching
        var snapshotsByUser = snapshots.GroupBy(s => s.UserId);

        foreach (var userGroup in snapshotsByUser)
        {
            var batch = new EntityChangedBatchEvent
            {
                CorrelationId = Activity.Current?.Id,
            };

            foreach (var snapshot in userGroup)
            {
                batch.Events.Add(new EntityChangedEvent
                {
                    EntityType = snapshot.EntityType,
                    EntityId = snapshot.EntityId,
                    Operation = snapshot.Operation,
                    UserId = snapshot.UserId,
                    DomainEventTypes = snapshot.DomainEventTypes,
                    CreatedAt = _dateTimeProvider.UtcNow
                });
            }

            try
            {
                await _messageBus.PublishAsync(
                    _serviceBusOptions.Value.EmbeddingsQueueName,
                    batch,
                    cancellationToken);

                _logger.LogDebug(
                    "Published batch of {Count} entity changes to Service Bus for user {UserId}",
                    batch.Events.Count, userGroup.Key);
            }
            catch (Exception ex)
            {
                // Log but don't fail - Service Bus DLQ will handle retries
                _logger.LogError(ex,
                    "Failed to publish batch of {Count} entities to Service Bus for user {UserId}",
                    userGroup.Count(), userGroup.Key);
            }
        }
    }

    /// <summary>
    /// Checks if any modified property is NOT marked with [EmbeddingIgnore].
    /// Returns true if at least one embedding-relevant property changed.
    /// </summary>
    private static bool HasEmbeddingRelevantChanges(EntityEntry entry)
    {
        var entityType = entry.Entity.GetType();

        var isClassIgnored = entityType.GetCustomAttribute<EmbeddingIgnoreAttribute>() is not null;
        if (!isClassIgnored)
        {
            foreach (var property in entry.Properties)
            {
                if (!property.IsModified)
                    continue;

                var propertyInfo = entityType.GetProperty(property.Metadata.Name);
                if (propertyInfo?.GetCustomAttribute<EmbeddingIgnoreAttribute>() is null)
                    return true;
            }
        }


        return false;
    }
}
