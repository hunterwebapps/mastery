using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Queries.GetTodayHabits;

public sealed class GetTodayHabitsQueryHandler : IQueryHandler<GetTodayHabitsQuery, List<TodayHabitDto>>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetTodayHabitsQueryHandler(
        IHabitRepository habitRepository,
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService)
    {
        _habitRepository = habitRepository;
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<TodayHabitDto>> Handle(GetTodayHabitsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get today's habits with occurrences
        var habits = await _habitRepository.GetTodayHabitsAsync(userId, today, cancellationToken);

        // Get goals to map habit impact
        var goals = await _goalRepository.GetActiveGoalsByUserIdAsync(userId, cancellationToken);
        var goalNameMap = goals.ToDictionary(g => g.Id, g => g.Title);

        var result = new List<TodayHabitDto>();

        foreach (var habit in habits)
        {
            var todayOccurrence = habit.Occurrences.FirstOrDefault(o => o.ScheduledOn == today);

            // Map goal IDs to goal names
            var goalImpactTags = habit.GoalIds
                .Where(gid => goalNameMap.ContainsKey(gid))
                .Select(gid => goalNameMap[gid])
                .ToList();

            result.Add(new TodayHabitDto
            {
                Id = habit.Id,
                Title = habit.Title,
                Description = habit.Description,
                IsDue = habit.IsDueOn(today),
                DefaultMode = habit.DefaultMode.ToString(),
                TodayOccurrence = todayOccurrence != null ? MapOccurrence(todayOccurrence) : null,
                Variants = habit.Variants.Select(MapVariant).ToList(),
                CurrentStreak = habit.CurrentStreak,
                AdherenceRate7Day = habit.AdherenceRate7Day,
                GoalImpactTags = goalImpactTags,
                RequiresValueEntry = habit.RequiresValueEntry,
                DisplayOrder = habit.DisplayOrder
            });
        }

        return result.OrderBy(h => h.DisplayOrder).ToList();
    }

    private static HabitOccurrenceDto MapOccurrence(Domain.Entities.Habit.HabitOccurrence occurrence)
    {
        return new HabitOccurrenceDto
        {
            Id = occurrence.Id,
            HabitId = occurrence.HabitId,
            ScheduledOn = occurrence.ScheduledOn.ToString("yyyy-MM-dd"),
            Status = occurrence.Status.ToString(),
            CompletedAt = occurrence.CompletedAt,
            CompletedOn = occurrence.CompletedOn?.ToString("yyyy-MM-dd"),
            ModeUsed = occurrence.ModeUsed?.ToString(),
            EnteredValue = occurrence.EnteredValue,
            MissReason = occurrence.MissReason?.ToString(),
            Note = occurrence.Note,
            RescheduledTo = occurrence.RescheduledTo?.ToString("yyyy-MM-dd")
        };
    }

    private static HabitVariantDto MapVariant(Domain.Entities.Habit.HabitVariant variant)
    {
        return new HabitVariantDto
        {
            Id = variant.Id,
            Mode = variant.Mode.ToString(),
            Label = variant.Label,
            DefaultValue = variant.DefaultValue,
            EstimatedMinutes = variant.EstimatedMinutes,
            EnergyCost = variant.EnergyCost,
            CountsAsCompletion = variant.CountsAsCompletion
        };
    }
}
