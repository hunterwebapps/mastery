using System.Security.Claims;

namespace Mastery.Infrastructure.Identity.Services;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    RefreshToken GenerateRefreshToken(string userId, string? ipAddress);
    ClaimsPrincipal? ValidateToken(string token);
}
