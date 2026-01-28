using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Habits.Commands.CreateHabit;

public sealed class CreateHabitCommandHandler : ICommandHandler<CreateHabitCommand, Guid>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHabitCommandHandler(
        IHabitRepository habitRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateHabitCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Parse default mode
        if (!Enum.TryParse<HabitMode>(request.DefaultMode, out var defaultMode))
            throw new DomainException($"Invalid default mode: {request.DefaultMode}");

        // Create schedule
        var schedule = CreateSchedule(request.Schedule);

        // Create policy
        var policy = CreatePolicy(request.Policy);

        // Get next display order
        var maxOrder = await _habitRepository.GetMaxDisplayOrderAsync(userId, cancellationToken);

        // Create the habit
        var habit = Habit.Create(
            userId: userId,
            title: request.Title,
            schedule: schedule,
            description: request.Description,
            why: request.Why,
            policy: policy,
            defaultMode: defaultMode,
            displayOrder: maxOrder + 1,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds,
            goalIds: request.GoalIds);

        // Add metric bindings if provided
        if (request.MetricBindings?.Count > 0)
        {
            var metricDefIds = request.MetricBindings.Select(b => b.MetricDefinitionId).Distinct().ToList();
            var metricDefs = await _metricDefinitionRepository.GetByIdsAsync(metricDefIds, cancellationToken);

            foreach (var bindingInput in request.MetricBindings)
            {
                var metricDef = metricDefs.FirstOrDefault(m => m.Id == bindingInput.MetricDefinitionId)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Metrics.MetricDefinition), bindingInput.MetricDefinitionId);

                if (metricDef.UserId != userId)
                    throw new DomainException($"Metric definition {bindingInput.MetricDefinitionId} does not belong to the current user.");

                if (!Enum.TryParse<HabitContributionType>(bindingInput.ContributionType, out var contributionType))
                    throw new DomainException($"Invalid contribution type: {bindingInput.ContributionType}");

                habit.AddMetricBinding(
                    metricDefinitionId: bindingInput.MetricDefinitionId,
                    contributionType: contributionType,
                    fixedValue: bindingInput.FixedValue,
                    notes: bindingInput.Notes);
            }
        }

        // Add variants if provided
        if (request.Variants?.Count > 0)
        {
            foreach (var variantInput in request.Variants)
            {
                if (!Enum.TryParse<HabitMode>(variantInput.Mode, out var mode))
                    throw new DomainException($"Invalid variant mode: {variantInput.Mode}");

                habit.AddVariant(
                    mode: mode,
                    label: variantInput.Label,
                    defaultValue: variantInput.DefaultValue,
                    estimatedMinutes: variantInput.EstimatedMinutes,
                    energyCost: variantInput.EnergyCost,
                    countsAsCompletion: variantInput.CountsAsCompletion);
            }
        }

        await _habitRepository.AddAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return habit.Id;
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

    private static HabitPolicy CreatePolicy(CreateHabitPolicyInput? input)
    {
        if (input == null)
            return HabitPolicy.Default();

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
