using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = Mastery.Domain.Entities.Task.Task;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Resolves entities by type and ID for outbox processing.
/// </summary>
public class EntityResolver(MasteryDbContext _context) : IEntityResolver
{
    public async Task<object?> ResolveAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        return entityType switch
        {
            nameof(Goal) => await _context.Goals
                .Include(x => x.Metrics)
                .SingleOrDefaultAsync(x => x.Id == entityId, ct),
            nameof(MetricDefinition) => await _context.MetricDefinitions.FindAsync([entityId], ct),
            nameof(Habit) => await _context.Habits
                .Include(x => x.MetricBindings)
                .Include(x => x.Variants)
                .SingleOrDefaultAsync(x => x.Id == entityId, ct),
            nameof(Task) => await _context.Tasks
                .Include(x => x.MetricBindings)
                .SingleOrDefaultAsync(x => x.Id == entityId, ct),
            nameof(Project) => await _context.Projects.FindAsync([entityId], ct),
            nameof(CheckIn) => await _context.CheckIns.FindAsync([entityId], ct),
            nameof(Experiment) => await _context.Experiments
                .Include(x => x.Result)
                .Include(x => x.Notes)
                .SingleOrDefaultAsync(x => x.Id == entityId, ct),
            nameof(UserProfile) => await _context.UserProfiles
                .Include(x => x.CurrentSeason)
                .SingleOrDefaultAsync(x => x.Id == entityId, ct),
            nameof(Season) => await _context.Seasons.FindAsync([entityId], ct),
            nameof(Recommendation) => await _context.Recommendations.FindAsync([entityId], ct),
            _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, null)
        };
    }
}
