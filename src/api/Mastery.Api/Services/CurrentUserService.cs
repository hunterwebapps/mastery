using System.Security.Claims;
using Mastery.Application.Common.Interfaces;

namespace Mastery.Api.Services;

public class CurrentUserService(IHttpContextAccessor _httpContextAccessor) : ICurrentUserService
{
    private const string SystemUserId = "system";
    private const string SystemUserName = "Background Worker";

    public string? UserId
    {
        get
        {
            // Try to get from JWT claims first
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(claimValue))
                return claimValue;

            // No HTTP context: background worker context
            if (_httpContextAccessor.HttpContext is null)
                return SystemUserId;

            return null;
        }
    }

    public string? UserName
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(claimValue))
                return claimValue;

            if (_httpContextAccessor.HttpContext is null)
                return SystemUserName;

            return null;
        }
    }
}
