using Mastery.Domain.Entities.Learning;

namespace Mastery.Application.Common.Interfaces;

public interface IUserPlaybookRepository
{
    Task<UserPlaybook?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(UserPlaybook playbook, CancellationToken ct = default);
    Task UpdateAsync(UserPlaybook playbook, CancellationToken ct = default);
}

public interface IInterventionOutcomeRepository
{
    Task AddAsync(InterventionOutcome outcome, CancellationToken ct = default);
    Task UpdateAsync(InterventionOutcome outcome, CancellationToken ct = default);
    Task<IReadOnlyList<InterventionOutcome>> GetByUserIdAsync(
        string userId,
        int limit = 100,
        CancellationToken ct = default);
    Task<IReadOnlyList<InterventionOutcome>> GetByRecommendationIdAsync(
        Guid recommendationId,
        CancellationToken ct = default);
    Task<InterventionOutcome?> GetByRecommendationIdSingleAsync(
        Guid recommendationId,
        CancellationToken ct = default);
}
