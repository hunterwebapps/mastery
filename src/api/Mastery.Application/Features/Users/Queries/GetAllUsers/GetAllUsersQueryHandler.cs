using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Users.Models;

namespace Mastery.Application.Features.Users.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, PaginatedList<UserListDto>>
{
    private readonly IUserManagementService _userManagementService;

    public GetAllUsersQueryHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<PaginatedList<UserListDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userManagementService.GetAllUsersAsync(
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
