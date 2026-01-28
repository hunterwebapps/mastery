using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Queries.GetHabitHistory;

public sealed class GetHabitHistoryQueryHandler : IQueryHandler<GetHabitHistoryQuery, HabitHistoryDto>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetHabitHistoryQueryHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
    }

    public async Task<HabitHistoryDto> Handle(GetHabitHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Parse dates
        if (!DateOnly.TryParse(request.FromDate, out var fromDate))
            throw new DomainException($"Invalid from date format: {request.FromDate}");

        if (!DateOnly.TryParse(request.ToDate, out var toDate))
            throw new DomainException($"Invalid to date format: {request.ToDate}");

        if (fromDate > toDate)
            throw new DomainException("From date must be before or equal to to date.");

        // Get habit with occurrences in range
        var habit = await _habitRepository.GetByIdWithOccurrencesAsync(
            request.HabitId,
            fromDate,
            toDate,
            cancellationToken);

        if (habit == null)
            throw new NotFoundException("Habit", request.HabitId);

        if (habit.UserId != userId)
            throw new DomainException("Habit does not belong to the current user.");

        // Calculate expected due count based on schedule
        var totalDue = habit.Schedule.GetExpectedCountInRange(fromDate, toDate);

        // Get occurrence stats
        var occurrences = habit.Occurrences.ToList();
        var totalCompleted = occurrences.Count(o => o.Status == HabitOccurrenceStatus.Completed);
        var totalMissed = occurrences.Count(o => o.Status == HabitOccurrenceStatus.Missed);
        var totalSkipped = occurrences.Count(o => o.Status == HabitOccurrenceStatus.Skipped);

        return new HabitHistoryDto
        {
            HabitId = habit.Id,
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            ToDate = toDate.ToString("yyyy-MM-dd"),
            Occurrences = occurrences.Select(o => new HabitOccurrenceDto
            {
                Id = o.Id,
                HabitId = o.HabitId,
                ScheduledOn = o.ScheduledOn.ToString("yyyy-MM-dd"),
                Status = o.Status.ToString(),
                CompletedAt = o.CompletedAt,
                CompletedOn = o.CompletedOn?.ToString("yyyy-MM-dd"),
                ModeUsed = o.ModeUsed?.ToString(),
                EnteredValue = o.EnteredValue,
                MissReason = o.MissReason?.ToString(),
                Note = o.Note,
                RescheduledTo = o.RescheduledTo?.ToString("yyyy-MM-dd")
            }).OrderByDescending(o => o.ScheduledOn).ToList(),
            TotalDue = totalDue,
            TotalCompleted = totalCompleted,
            TotalMissed = totalMissed,
            TotalSkipped = totalSkipped
        };
    }
}
