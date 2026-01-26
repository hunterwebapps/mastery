using System.Security.Claims;
using Mastery.Application.Common.Interfaces;

namespace Mastery.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // TODO: Remove this shim when real authentication is implemented
    private const string DevUserId = "dev-user-001";
    private const string DevUserName = "Developer";

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? DevUserId; // Fallback to dev user when not authenticated

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? DevUserName; // Fallback to dev user when not authenticated
}
