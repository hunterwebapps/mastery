using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Identity.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment _environment;

    public EmailService(ILogger<EmailService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "DEV: Password reset email for {Email}. Reset link: {ResetLink}",
                email,
                resetLink);
            return Task.CompletedTask;
        }

        // TODO: Implement production email sending (SendGrid, SES, etc.)
        _logger.LogWarning(
            "Email sending not configured for production. Would send password reset to {Email}",
            email);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string? displayName)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "DEV: Welcome email for {Email} ({DisplayName})",
                email,
                displayName ?? "User");
            return Task.CompletedTask;
        }

        // TODO: Implement production email sending
        _logger.LogWarning(
            "Email sending not configured for production. Would send welcome email to {Email}",
            email);
        return Task.CompletedTask;
    }
}
