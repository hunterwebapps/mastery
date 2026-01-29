using Microsoft.AspNetCore.Identity;

namespace Mastery.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string AuthProvider { get; set; } = "Email";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsDisabled { get; set; } = false;
}
