using Mastery.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Identity.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly MasteryDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        MasteryDbContext dbContext,
        IJwtService jwtService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResult(false, Error: "An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            AuthProvider = "Email",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
            return new AuthResult(false, Error: errors);
        }

        // Assign default User role
        await _userManager.AddToRoleAsync(user, AppRoles.User);

        _logger.LogInformation("User {Email} registered successfully", request.Email);
        return await GenerateAuthResultAsync(user, ipAddress);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return new AuthResult(false, Error: "Invalid email or password.");
        }

        if (user.IsDisabled)
        {
            _logger.LogWarning("Disabled user {Email} attempted to login", request.Email);
            return new AuthResult(false, Error: "Your account has been disabled. Please contact support.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} is locked out", request.Email);
            return new AuthResult(false, Error: "Account is locked. Please try again later.");
        }

        if (!result.Succeeded)
        {
            return new AuthResult(false, Error: "Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);
        return await GenerateAuthResultAsync(user, ipAddress);
    }

    public async Task<AuthResult> ExternalLoginCallbackAsync(ExternalLoginInfo info, string? ipAddress)
    {
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return new AuthResult(false, Error: "Email not provided by external provider.");
        }

        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var displayName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? info.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = displayName,
                    AuthProvider = info.LoginProvider,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return new AuthResult(false, Error: errors);
                }

                // Assign default User role
                await _userManager.AddToRoleAsync(user, AppRoles.User);

                _logger.LogInformation("User {Email} created via {Provider}", email, info.LoginProvider);
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                return new AuthResult(false, Error: errors);
            }
        }

        if (user.IsDisabled)
        {
            _logger.LogWarning("Disabled user {Email} attempted to login via {Provider}", email, info.LoginProvider);
            return new AuthResult(false, Error: "Your account has been disabled. Please contact support.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in via {Provider}", email, info.LoginProvider);
        return await GenerateAuthResultAsync(user, ipAddress);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var storedToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (storedToken == null)
        {
            return new AuthResult(false, Error: "Invalid refresh token.");
        }

        if (!storedToken.IsActive)
        {
            return new AuthResult(false, Error: "Refresh token has expired or been revoked.");
        }

        if (storedToken.User.IsDisabled)
        {
            return new AuthResult(false, Error: "Your account has been disabled. Please contact support.");
        }

        // Rotate the refresh token
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;

        var newRefreshToken = _jwtService.GenerateRefreshToken(storedToken.UserId, ipAddress);
        storedToken.ReplacedByToken = newRefreshToken.Token;

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(storedToken.User);
        var accessToken = _jwtService.GenerateAccessToken(storedToken.User, roles);
        var hasProfile = await UserHasProfileAsync(storedToken.UserId);

        return new AuthResult(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: newRefreshToken.Token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            User: new UserInfoDto(
                storedToken.User.Id,
                storedToken.User.Email ?? string.Empty,
                storedToken.User.DisplayName,
                hasProfile,
                storedToken.User.AuthProvider,
                roles.ToList()
            )
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (storedToken != null && storedToken.IsActive)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that the user doesn't exist
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"https://mastery.app/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendPasswordResetEmailAsync(email, resetLink);
        _logger.LogInformation("Password reset requested for {Email}", email);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successful for {Email}", email);
            return true;
        }

        _logger.LogWarning("Password reset failed for {Email}: {Errors}",
            email, string.Join(", ", result.Errors.Select(e => e.Description)));
        return false;
    }

    public async Task<bool> UserHasProfileAsync(string userId)
    {
        return await _dbContext.UserProfiles
            .AnyAsync(p => p.UserId == userId);
    }

    private async Task<AuthResult> GenerateAuthResultAsync(ApplicationUser user, string? ipAddress)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id, ipAddress);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        var hasProfile = await UserHasProfileAsync(user.Id);

        return new AuthResult(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15),
            User: new UserInfoDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                hasProfile,
                user.AuthProvider,
                roles.ToList()
            )
        );
    }
}
