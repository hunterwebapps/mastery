namespace Mastery.Infrastructure.Identity;

public record AuthResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null,
    UserInfoDto? User = null,
    string? Error = null
);

public record UserInfoDto(
    string Id,
    string Email,
    string? DisplayName,
    bool HasProfile,
    string AuthProvider,
    List<string> Roles
);
