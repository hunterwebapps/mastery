namespace Mastery.Infrastructure.Identity.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendWelcomeEmailAsync(string email, string? displayName);
}
