using System.Security.Claims;

namespace Mastery.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return "8a57fb96-9356-4bc5-8938-6415665ccce0";
        // return claimsPrincipal.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value;
    }
}
