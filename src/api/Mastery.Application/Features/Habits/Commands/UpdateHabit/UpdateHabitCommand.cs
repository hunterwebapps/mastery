using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Habits.Commands.CreateHabit;

namespace Mastery.Application.Features.Habits.Commands.UpdateHabit;

/// <summary>
/// Updates a habit's details.
/// </summary>
public sealed record UpdateHabitCommand(
    Guid HabitId,
    string? Title = null,
    string? Description = null,
    string? Why = null,
    string? DefaultMode = null,
    CreateHabitScheduleInput? Schedule = null,
    CreateHabitPolicyInput? Policy = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? GoalIds = null) : ICommand;
