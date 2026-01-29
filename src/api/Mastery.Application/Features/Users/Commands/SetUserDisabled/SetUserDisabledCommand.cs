using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Users.Commands.SetUserDisabled;

public sealed record SetUserDisabledCommand(string UserId, bool Disabled) : ICommand<SetUserDisabledResult>;

public sealed record SetUserDisabledResult(bool Success, string? Error = null);
