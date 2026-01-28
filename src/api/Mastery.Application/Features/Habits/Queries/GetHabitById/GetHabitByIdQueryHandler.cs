using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Queries.GetHabitById;

public sealed class GetHabitByIdQueryHandler : IQueryHandler<GetHabitByIdQuery, HabitDto>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetHabitByIdQueryHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
    }

    public async Task<HabitDto> Handle(GetHabitByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var habit = await _habitRepository.GetByIdWithDetailsAsync(request.HabitId, cancellationToken);

        if (habit == null)
            throw new NotFoundException("Habit", request.HabitId);

        if (habit.UserId != userId)
            throw new DomainException("Habit does not belong to the current user.");

        return MapToDto(habit);
    }

    private static HabitDto MapToDto(Habit habit)
    {
        return new HabitDto
        {
            Id = habit.Id,
            UserId = habit.UserId,
            Title = habit.Title,
            Description = habit.Description,
            Why = habit.Why,
            Status = habit.Status.ToString(),
            DisplayOrder = habit.DisplayOrder,
            Schedule = new HabitScheduleDto
            {
                Type = habit.Schedule.Type.ToString(),
                DaysOfWeek = habit.Schedule.DaysOfWeek?.Select(d => (int)d).ToList(),
                PreferredTimes = habit.Schedule.PreferredTimes?.Select(t => t.ToString("HH:mm")).ToList(),
                FrequencyPerWeek = habit.Schedule.FrequencyPerWeek,
                IntervalDays = habit.Schedule.IntervalDays,
                StartDate = habit.Schedule.StartDate.ToString("yyyy-MM-dd"),
                EndDate = habit.Schedule.EndDate?.ToString("yyyy-MM-dd")
            },
            Policy = new HabitPolicyDto
            {
                AllowLateCompletion = habit.Policy.AllowLateCompletion,
                LateCutoffTime = habit.Policy.LateCutoffTime?.ToString("HH:mm"),
                AllowSkip = habit.Policy.AllowSkip,
                RequireMissReason = habit.Policy.RequireMissReason,
                AllowBackfill = habit.Policy.AllowBackfill,
                MaxBackfillDays = habit.Policy.MaxBackfillDays
            },
            DefaultMode = habit.DefaultMode.ToString(),
            RoleIds = habit.RoleIds.ToList(),
            ValueIds = habit.ValueIds.ToList(),
            GoalIds = habit.GoalIds.ToList(),
            MetricBindings = habit.MetricBindings.Select(b => new HabitMetricBindingDto
            {
                Id = b.Id,
                MetricDefinitionId = b.MetricDefinitionId,
                ContributionType = b.ContributionType.ToString(),
                FixedValue = b.FixedValue,
                Notes = b.Notes
            }).ToList(),
            Variants = habit.Variants.Select(v => new HabitVariantDto
            {
                Id = v.Id,
                Mode = v.Mode.ToString(),
                Label = v.Label,
                DefaultValue = v.DefaultValue,
                EstimatedMinutes = v.EstimatedMinutes,
                EnergyCost = v.EnergyCost,
                CountsAsCompletion = v.CountsAsCompletion
            }).ToList(),
            CurrentStreak = habit.CurrentStreak,
            AdherenceRate7Day = habit.AdherenceRate7Day,
            CreatedAt = habit.CreatedAt,
            ModifiedAt = habit.ModifiedAt
        };
    }
}
