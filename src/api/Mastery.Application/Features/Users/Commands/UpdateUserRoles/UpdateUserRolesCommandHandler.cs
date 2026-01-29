using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Users.Commands.UpdateUserRoles;

public sealed class UpdateUserRolesCommandHandler : ICommandHandler<UpdateUserRolesCommand, UpdateUserRolesResult>
{
    private readonly IUserManagementService _userManagementService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserRolesCommandHandler(
        IUserManagementService userManagementService,
        ICurrentUserService currentUserService)
    {
        _userManagementService = userManagementService;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateUserRolesResult> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return new UpdateUserRolesResult(false, "Not authenticated");
        }

        var (success, error) = await _userManagementService.UpdateUserRolesAsync(
            request.UserId,
            request.Roles,
            currentUserId,
            cancellationToken);

        return new UpdateUserRolesResult(success, error);
    }
}
