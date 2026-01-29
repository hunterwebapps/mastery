using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Users.Models;

namespace Mastery.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly IUserManagementService _userManagementService;

    public GetUserByIdQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<UserDetailDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _userManagementService.GetUserByIdAsync(request.UserId, cancellationToken);
    }
}
