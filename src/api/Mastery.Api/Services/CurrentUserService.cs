using System.Security.Claims;
using Mastery.Application.Common.Interfaces;

namespace Mastery.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;

    // TODO: Remove this shim when real authentication is implemented
    private const string DevUserId = "dev-user-001";
    private const string DevUserName = "Developer";
    private const string SystemUserId = "system";
    private const string SystemUserName = "Background Worker";

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public string? UserId
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claimValue is not null) return claimValue;

            // No HTTP context: background worker in production, or dev fallback
            return _environment.IsDevelopment() ? DevUserId : SystemUserId;
        }
    }

    public string? UserName
    {
        get
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
            if (claimValue is not null) return claimValue;

            return _environment.IsDevelopment() ? DevUserName : SystemUserName;
        }
    }
}
