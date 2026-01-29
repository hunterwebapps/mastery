using Microsoft.AspNetCore.Identity;

namespace Mastery.Infrastructure.Identity.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResult> ExternalLoginCallbackAsync(ExternalLoginInfo info, string? ipAddress);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress);
    Task RevokeTokenAsync(string refreshToken, string? ipAddress);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task<bool> UserHasProfileAsync(string userId);
}

public record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName
);

public record LoginRequest(
    string Email,
    string Password
);
