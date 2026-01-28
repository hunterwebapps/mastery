using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Queries.GetHabits;

public sealed class GetHabitsQueryHandler : IQueryHandler<GetHabitsQuery, List<HabitSummaryDto>>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetHabitsQueryHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<HabitSummaryDto>> Handle(GetHabitsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        IReadOnlyList<Habit> habits;

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (!Enum.TryParse<HabitStatus>(request.Status, out var status))
                throw new DomainException($"Invalid status: {request.Status}");

            habits = await _habitRepository.GetByStatusAsync(userId, status, cancellationToken);
        }
        else
        {
            habits = await _habitRepository.GetByUserIdAsync(userId, cancellationToken);
        }

        return habits.Select(MapToSummary).ToList();
    }

    private static HabitSummaryDto MapToSummary(Habit habit)
    {
        return new HabitSummaryDto
        {
            Id = habit.Id,
            Title = habit.Title,
            Description = habit.Description,
            Status = habit.Status.ToString(),
            DefaultMode = habit.DefaultMode.ToString(),
            DisplayOrder = habit.DisplayOrder,
            ScheduleType = habit.Schedule.Type.ToString(),
            ScheduleDescription = habit.Schedule.ToString(),
            MetricBindingCount = habit.MetricBindings.Count,
            VariantCount = habit.Variants.Count,
            CurrentStreak = habit.CurrentStreak,
            AdherenceRate7Day = habit.AdherenceRate7Day,
            CreatedAt = habit.CreatedAt
        };
    }
}
