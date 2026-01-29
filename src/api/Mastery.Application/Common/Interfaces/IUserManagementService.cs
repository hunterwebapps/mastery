using Mastery.Application.Features.Users.Models;

namespace Mastery.Application.Common.Interfaces;

public interface IUserManagementService
{
    Task<PaginatedList<UserListDto>> GetAllUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<UserDetailDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken);
    Task<(bool Success, string? Error)> UpdateUserRolesAsync(string userId, List<string> roles, string currentUserId, CancellationToken cancellationToken);
    Task<(bool Success, string? Error)> SetUserDisabledAsync(string userId, bool disabled, string currentUserId, CancellationToken cancellationToken);
}
