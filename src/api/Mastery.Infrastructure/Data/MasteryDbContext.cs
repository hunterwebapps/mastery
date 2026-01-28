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
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Data;

public class MasteryDbContext : DbContext, IMasteryDbContext, IUnitOfWork
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
    public DbSet<DiagnosticSignal> DiagnosticSignals => Set<DiagnosticSignal>();
    public DbSet<RecommendationRunHistory> RecommendationRunHistory => Set<RecommendationRunHistory>();

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
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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

        return await base.SaveChangesAsync(cancellationToken);
    }
}
