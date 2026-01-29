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
using Mastery.Infrastructure.Outbox;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mastery.Infrastructure.Data;

public class MasteryDbContext : IdentityDbContext<ApplicationUser>, IMasteryDbContext, IUnitOfWork
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

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
    public DbSet<Domain.Entities.Task.Task> Tasks => Set<Domain.Entities.Task.Task>();
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

    public MasteryDbContext(
        DbContextOptions<MasteryDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

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

        // 2. Apply audit fields
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

        // 3. Add outbox entries to the same transaction
        if (outboxEntries.Count > 0)
        {
            await OutboxEntries.AddRangeAsync(outboxEntries, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Captures entity changes for aggregate roots to be processed by the outbox worker.
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

            // Determine operation type
            var operation = entry.State switch
            {
                EntityState.Added when !isClassIgnored => "Created",
                EntityState.Modified when !isClassIgnored && HasEmbeddingRelevantChanges(entry) => "Updated",
                EntityState.Deleted => "Deleted",
                _ => null
            };

            if (operation is null)
            {
                continue;
            }

            string? userId = null;
            if (entity is OwnedEntity ownedEntity)
            {
                userId = ownedEntity.UserId;
            }

            entries.Add(OutboxEntry.Create(
                entityType.Name,
                entity.Id,
                operation,
                userId,
                now));
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
