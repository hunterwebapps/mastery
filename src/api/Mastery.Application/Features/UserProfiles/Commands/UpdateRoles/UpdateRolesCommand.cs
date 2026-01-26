using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.UserProfiles.Models;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateRoles;

/// <summary>
/// Updates the current user's roles.
/// </summary>
public sealed record UpdateRolesCommand(List<UserRoleDto> Roles) : ICommand;
