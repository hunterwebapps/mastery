namespace Mastery.Api.Contracts.Auth;

public record LoginRequest(
    string Email,
    string Password
);
