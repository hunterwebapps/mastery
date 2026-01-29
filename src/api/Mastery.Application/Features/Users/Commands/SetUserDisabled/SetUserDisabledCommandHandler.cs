using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Users.Commands.SetUserDisabled;

public sealed class SetUserDisabledCommandHandler : ICommandHandler<SetUserDisabledCommand, SetUserDisabledResult>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public SetUserDisabledCommandHandler(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    public async Task<SetUserDisabledResult> Handle(SetUserDisabledCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return new SetUserDisabledResult(false, "Not authenticated");
        }

        var (success, error) = await _userManagementService.SetUserDisabledAsync(
            request.UserId,
            request.Disabled,
            currentUserId,
            cancellationToken);

        return new SetUserDisabledResult(success, error);
    }
}
