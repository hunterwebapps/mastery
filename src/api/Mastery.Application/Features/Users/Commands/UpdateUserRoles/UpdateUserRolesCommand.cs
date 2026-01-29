using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Users.Commands.UpdateUserRoles;

public sealed record UpdateUserRolesCommand(string UserId, List<string> Roles) : ICommand<UpdateUserRolesResult>;

public sealed record UpdateUserRolesResult(bool Success, string? Error = null);
