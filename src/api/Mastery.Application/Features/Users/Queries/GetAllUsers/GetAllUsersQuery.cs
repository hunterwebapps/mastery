using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Users.Models;

namespace Mastery.Application.Features.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IQuery<PaginatedList<UserListDto>>;
