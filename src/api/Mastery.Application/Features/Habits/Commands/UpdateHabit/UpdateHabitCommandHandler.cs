using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Commands.CreateHabit;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Habits.Commands.UpdateHabit;

public sealed class UpdateHabitCommandHandler : ICommandHandler<UpdateHabitCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHabitCommandHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateHabitCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var habit = await _habitRepository.GetByIdWithDetailsAsync(request.HabitId, cancellationToken);

        if (habit == null)
            throw new NotFoundException("Habit", request.HabitId);

        if (habit.UserId != userId)
            throw new DomainException("Habit does not belong to the current user.");

        // Update basic details
        habit.Update(
            title: request.Title,
            description: request.Description,
            why: request.Why,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds,
            goalIds: request.GoalIds);

        // Update default mode if provided
        if (!string.IsNullOrEmpty(request.DefaultMode))
        {
            if (!Enum.TryParse<HabitMode>(request.DefaultMode, out var mode))
                throw new DomainException($"Invalid default mode: {request.DefaultMode}");
            habit.SetDefaultMode(mode);
        }

        // Update schedule if provided
        if (request.Schedule != null)
        {
            var schedule = CreateSchedule(request.Schedule);
            habit.UpdateSchedule(schedule);
        }

        // Update policy if provided
        if (request.Policy != null)
        {
            var policy = CreatePolicy(request.Policy);
            habit.UpdatePolicy(policy);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static HabitSchedule CreateSchedule(CreateHabitScheduleInput input)
    {
        if (!Enum.TryParse<ScheduleType>(input.Type, out var scheduleType))
            throw new DomainException($"Invalid schedule type: {input.Type}");

        DateOnly? startDate = null;
        DateOnly? endDate = null;

        if (!string.IsNullOrEmpty(input.StartDate) && DateOnly.TryParse(input.StartDate, out var sd))
            startDate = sd;

        if (!string.IsNullOrEmpty(input.EndDate) && DateOnly.TryParse(input.EndDate, out var ed))
            endDate = ed;

        DayOfWeek[]? daysOfWeek = null;
        if (input.DaysOfWeek?.Count > 0)
            daysOfWeek = input.DaysOfWeek.Select(d => (DayOfWeek)d).ToArray();

        TimeOnly[]? preferredTimes = null;
        if (input.PreferredTimes?.Count > 0)
            preferredTimes = input.PreferredTimes
                .Select(t => TimeOnly.TryParse(t, out var time) ? time : TimeOnly.MinValue)
                .Where(t => t != TimeOnly.MinValue)
                .ToArray();

        return HabitSchedule.Create(
            type: scheduleType,
            daysOfWeek: daysOfWeek,
            preferredTimes: preferredTimes,
            frequencyPerWeek: input.FrequencyPerWeek,
            intervalDays: input.IntervalDays,
            startDate: startDate,
            endDate: endDate);
    }

    private static HabitPolicy CreatePolicy(CreateHabitPolicyInput input)
    {
        TimeOnly? lateCutoff = null;
        if (!string.IsNullOrEmpty(input.LateCutoffTime) && TimeOnly.TryParse(input.LateCutoffTime, out var lc))
            lateCutoff = lc;

        return HabitPolicy.Create(
            allowLateCompletion: input.AllowLateCompletion,
            lateCutoffTime: lateCutoff,
            allowSkip: input.AllowSkip,
            requireMissReason: input.RequireMissReason,
            allowBackfill: input.AllowBackfill,
            maxBackfillDays: input.MaxBackfillDays);
    }
}
