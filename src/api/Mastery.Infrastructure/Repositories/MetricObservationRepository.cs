using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class MetricObservationRepository : IMetricObservationRepository
{
    private readonly MasteryDbContext _context;
    private readonly DbSet<MetricObservation> _dbSet;

    public MetricObservationRepository(MasteryDbContext context)
    {
        _context = context;
        _dbSet = context.Set<MetricObservation>();
    }

    public async Task<MetricObservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<MetricObservation> AddAsync(
        MetricObservation observation,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(observation, cancellationToken);
        return observation;
    }

    public async Task<IReadOnlyList<MetricObservation>> GetByMetricAndDateRangeAsync(
        Guid metricDefinitionId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(o => o.MetricDefinitionId == metricDefinitionId &&
                        o.UserId == userId &&
                        o.ObservedOn >= startDate &&
                        o.ObservedOn <= endDate);

        if (excludeCorrected)
        {
            query = query.Where(o => !o.IsCorrected);
        }

        return await query
            .OrderBy(o => o.ObservedOn)
            .ThenBy(o => o.ObservedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MetricObservation?> GetLatestByMetricAsync(
        Guid metricDefinitionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.MetricDefinitionId == metricDefinitionId &&
                        o.UserId == userId &&
                        !o.IsCorrected)
            .OrderByDescending(o => o.ObservedOn)
            .ThenByDescending(o => o.ObservedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricObservation>> GetByMetricsAndDateRangeAsync(
        IEnumerable<Guid> metricDefinitionIds,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default)
    {
        var idList = metricDefinitionIds.ToList();

        var query = _dbSet
            .Where(o => idList.Contains(o.MetricDefinitionId) &&
                        o.UserId == userId &&
                        o.ObservedOn >= startDate &&
                        o.ObservedOn <= endDate);

        if (excludeCorrected)
        {
            query = query.Where(o => !o.IsCorrected);
        }

        return await query
            .OrderBy(o => o.MetricDefinitionId)
            .ThenBy(o => o.ObservedOn)
            .ThenBy(o => o.ObservedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricObservation>> GetByUserAndDateAsync(
        string userId,
        DateOnly date,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(o => o.UserId == userId && o.ObservedOn == date);

        if (excludeCorrected)
        {
            query = query.Where(o => !o.IsCorrected);
        }

        return await query
            .OrderBy(o => o.ObservedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByMetricAndDateRangeAsync(
        Guid metricDefinitionId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(o => o.MetricDefinitionId == metricDefinitionId &&
                        o.UserId == userId &&
                        o.ObservedOn >= startDate &&
                        o.ObservedOn <= endDate);

        if (excludeCorrected)
        {
            query = query.Where(o => !o.IsCorrected);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<decimal?> GetAggregatedValueAsync(
        Guid metricDefinitionId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        MetricAggregation aggregation,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(o => o.MetricDefinitionId == metricDefinitionId &&
                        o.UserId == userId &&
                        o.ObservedOn >= startDate &&
                        o.ObservedOn <= endDate &&
                        !o.IsCorrected);

        if (!await query.AnyAsync(cancellationToken))
        {
            return null;
        }

        return aggregation switch
        {
            MetricAggregation.Sum => await query.SumAsync(o => o.Value, cancellationToken),
            MetricAggregation.Average => await query.AverageAsync(o => o.Value, cancellationToken),
            MetricAggregation.Max => await query.MaxAsync(o => o.Value, cancellationToken),
            MetricAggregation.Min => await query.MinAsync(o => o.Value, cancellationToken),
            MetricAggregation.Count => await query.CountAsync(cancellationToken),
            MetricAggregation.Latest => await query
                .OrderByDescending(o => o.ObservedOn)
                .ThenByDescending(o => o.ObservedAt)
                .Select(o => o.Value)
                .FirstOrDefaultAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, "Unknown aggregation type")
        };
    }

    public async Task<IReadOnlyList<MetricObservation>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.CorrelationId == correlationId && !o.IsCorrected)
            .ToListAsync(cancellationToken);
    }
}
