using Mastery.Api.Contracts.Auth;
using Mastery.Application.Features.UserProfiles.Commands.CreateUserProfile;
using Mastery.Application.Features.UserProfiles.Models;
using Mastery.Infrastructure.Identity;
using Mastery.Infrastructure.Identity.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISender _mediator;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        IAuthService authService,
        ISender mediator,
        SignInManager<ApplicationUser> signInManager)
    {
        _authService = authService;
        _mediator = mediator;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Register a new user with email and password.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] Contracts.Auth.RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterAsync(
            new Infrastructure.Identity.Services.RegisterRequest(
                request.Email,
                request.Password,
                request.DisplayName),
            ipAddress);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result);
    }

    /// <summary>
    /// Register a new user and create their profile atomically (combined onboarding completion).
    /// </summary>
    [HttpPost("register-with-profile")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterWithProfile(
        [FromBody] RegisterWithProfileRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();

        // First, register the user
        var authResult = await _authService.RegisterAsync(
            new Infrastructure.Identity.Services.RegisterRequest(
                request.Email,
                request.Password,
                request.DisplayName),
            ipAddress);

        if (!authResult.Success)
        {
            return BadRequest(new { error = authResult.Error });
        }

        // Then create the profile using the new user's ID
        try
        {
            var initialSeason = request.InitialSeason is not null
                ? new InitialSeasonDto(
                    request.InitialSeason.Label,
                    request.InitialSeason.Type,
                    request.InitialSeason.StartDate,
                    request.InitialSeason.ExpectedEndDate,
                    request.InitialSeason.FocusRoleIds,
                    request.InitialSeason.FocusGoalIds,
                    request.InitialSeason.SuccessStatement,
                    request.InitialSeason.NonNegotiables,
                    request.InitialSeason.Intensity)
                : null;

            var command = new CreateUserProfileCommand(
                request.Timezone,
                request.Locale,
                request.Values,
                request.Roles,
                request.Preferences,
                request.Constraints,
                initialSeason,
                authResult.User!.Id); // Pass the user ID explicitly

            await _mediator.Send(command, cancellationToken);

            // Update the auth result to reflect that user now has a profile
            return Ok(authResult with
            {
                User = authResult.User with { HasProfile = true }
            });
        }
        catch (Exception)
        {
            // Profile creation failed, but user was already created
            // Return auth result without profile (user can retry profile creation)
            return Ok(authResult);
        }
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] Contracts.Auth.LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(
            new Infrastructure.Identity.Services.LoginRequest(request.Email, request.Password),
            ipAddress);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result);
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout and revoke refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);
        return NoContent();
    }

    /// <summary>
    /// Request password reset email.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        // Always return success to prevent email enumeration
        return Ok(new { message = "If an account exists with this email, a reset link will be sent." });
    }

    /// <summary>
    /// Reset password using token from email.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _authService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword);

        if (!success)
        {
            return BadRequest(new { error = "Invalid or expired reset token." });
        }

        return Ok(new { message = "Password has been reset successfully." });
    }

    /// <summary>
    /// Get current authenticated user info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var authProvider = User.FindFirst("auth_provider")?.Value ?? "Email";
        var hasProfile = await _authService.UserHasProfileAsync(userId);
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new UserInfoDto(userId, email, name, hasProfile, authProvider, roles));
    }

    /// <summary>
    /// Initiate external OAuth login.
    /// </summary>
    [HttpGet("external/{provider}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult ExternalLogin(string provider, [FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Handle OAuth callback.
    /// </summary>
    [HttpGet("external/callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var frontendUrl = GetFrontendUrl();
        var callbackUrl = $"{frontendUrl}/auth/callback";

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Redirect($"{callbackUrl}?error={Uri.EscapeDataString("External login failed. Please try again.")}");
        }

        var ipAddress = GetIpAddress();
        var result = await _authService.ExternalLoginCallbackAsync(info, ipAddress);

        if (!result.Success)
        {
            return Redirect($"{callbackUrl}?error={Uri.EscapeDataString(result.Error ?? "Unknown error occurred.")}");
        }

        // Build redirect URL with all required parameters for frontend
        var user = result.User!;
        var queryParams = new Dictionary<string, string?>
        {
            ["token"] = result.AccessToken,
            ["refresh"] = result.RefreshToken,
            ["hasProfile"] = user.HasProfile.ToString().ToLowerInvariant(),
            ["userId"] = user.Id,
            ["email"] = user.Email,
            ["displayName"] = user.DisplayName,
            ["provider"] = user.AuthProvider
        };

        var queryString = string.Join("&", queryParams
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}"));

        return Redirect($"{callbackUrl}?{queryString}");
    }

    private string? GetIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string GetFrontendUrl()
    {
        // TODO: Make this configurable
        return Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
    }
}
