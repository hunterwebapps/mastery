using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Common;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Entities.UserProfile;
using Microsoft.Extensions.DependencyInjection;
using Task = Mastery.Domain.Entities.Task.Task;

namespace Mastery.Infrastructure.Embeddings;

/// <summary>
/// Factory for resolving embedding text strategies by entity type.
/// Uses the service provider to resolve strongly-typed strategies.
/// </summary>
public sealed class EmbeddingTextStrategyFactory : IEmbeddingTextStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;

    public EmbeddingTextStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<string?> CompileTextAsync(string entityType, object entity, CancellationToken ct)
    {
        return entityType switch
        {
            nameof(Goal) => await CompileAsync<Goal>(entity, ct),
            nameof(Habit) => await CompileAsync<Habit>(entity, ct),
            nameof(Task) => await CompileAsync<Task>(entity, ct),
            nameof(Project) => await CompileAsync<Project>(entity, ct),
            nameof(CheckIn) => await CompileAsync<CheckIn>(entity, ct),
            nameof(Experiment) => await CompileAsync<Experiment>(entity, ct),
            nameof(UserProfile) => await CompileAsync<UserProfile>(entity, ct),
            nameof(Season) => await CompileAsync<Season>(entity, ct),
            nameof(MetricDefinition) => await CompileAsync<MetricDefinition>(entity, ct),
            nameof(Recommendation) => await CompileAsync<Recommendation>(entity, ct),
            _ => null
        };
    }

    public string? GetUserId(object entity)
    {
        return entity is OwnedEntity ownedEntity
            ? ownedEntity.UserId
            : null;
    }

    private async Task<string?> CompileAsync<T>(object entity, CancellationToken ct) where T : class
    {
        if (entity is not T typedEntity)
        {
            return null;
        }

        var strategy = _serviceProvider.GetService<IEmbeddingTextStrategy<T>>();
        if (strategy is null)
        {
            return null;
        }

        return await strategy.CompileTextAsync(typedEntity, ct);
    }
}
