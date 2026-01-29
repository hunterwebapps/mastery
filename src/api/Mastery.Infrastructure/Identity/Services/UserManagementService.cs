using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Users.Models;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Identity.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MasteryDbContext _dbContext;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        MasteryDbContext dbContext,
        IUserProfileRepository userProfileRepository,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    public async Task<PaginatedList<UserListDto>> GetAllUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                DisplayName = user.DisplayName,
                AuthProvider = user.AuthProvider,
                Roles = roles.ToList(),
                IsDisabled = user.IsDisabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        return new PaginatedList<UserListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var hasProfile = await _userProfileRepository.ExistsByUserIdAsync(userId, cancellationToken);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            DisplayName = user.DisplayName,
            AuthProvider = user.AuthProvider,
            Roles = roles.ToList(),
            IsDisabled = user.IsDisabled,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            EmailConfirmed = user.EmailConfirmed,
            HasProfile = hasProfile
        };
    }

    public async Task<(bool Success, string? Error)> UpdateUserRolesAsync(string userId, List<string> newRoles, string currentUserId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        // Check if current user is Super
        var currentUser = await _userManager.FindByIdAsync(currentUserId);
        if (currentUser == null)
        {
            return (false, "Current user not found");
        }

        var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
        var isCurrentUserSuper = currentUserRoles.Contains(AppRoles.Super);

        // Only Super users can assign/remove the Super role
        if (newRoles.Contains(AppRoles.Super) && !isCurrentUserSuper)
        {
            return (false, "Only Super users can assign the Super role");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(AppRoles.Super) && !newRoles.Contains(AppRoles.Super) && !isCurrentUserSuper)
        {
            return (false, "Only Super users can remove the Super role");
        }

        // Validate all roles exist
        foreach (var role in newRoles)
        {
            if (!AppRoles.All.Contains(role))
            {
                return (false, $"Invalid role: {role}");
            }
        }

        // Remove all current roles
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            _logger.LogError("Failed to remove roles from user {UserId}: {Errors}",
                userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            return (false, "Failed to update roles");
        }

        // Add new roles
        if (newRoles.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, newRoles);
            if (!addResult.Succeeded)
            {
                _logger.LogError("Failed to add roles to user {UserId}: {Errors}",
                    userId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                return (false, "Failed to update roles");
            }
        }

        _logger.LogInformation("User {AdminId} updated roles for user {UserId} to [{Roles}]",
            currentUserId, userId, string.Join(", ", newRoles));

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetUserDisabledAsync(string userId, bool disabled, string currentUserId, CancellationToken cancellationToken)
    {
        if (userId == currentUserId)
        {
            return (false, "You cannot disable your own account");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        user.IsDisabled = disabled;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogError("Failed to update disabled status for user {UserId}: {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return (false, "Failed to update user status");
        }

        _logger.LogInformation("User {AdminId} set disabled={Disabled} for user {UserId}",
            currentUserId, disabled, userId);

        return (true, null);
    }
}
