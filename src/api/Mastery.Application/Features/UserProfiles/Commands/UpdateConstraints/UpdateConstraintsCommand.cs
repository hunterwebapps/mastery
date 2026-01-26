using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateConstraints;

/// <summary>
/// Updates the current user's constraints.
/// </summary>
public sealed record UpdateConstraintsCommand(ConstraintsDto Constraints) : ICommand;
