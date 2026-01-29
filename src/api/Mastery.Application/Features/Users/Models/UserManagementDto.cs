namespace Mastery.Application.Features.Users.Models;

public record UserListDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
    public required string AuthProvider { get; init; }
    public required List<string> Roles { get; init; }
    public required bool IsDisabled { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record UserDetailDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
    public required string AuthProvider { get; init; }
    public required List<string> Roles { get; init; }
    public required bool IsDisabled { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool HasProfile { get; init; }
}

public record PaginatedList<T>
{
    public required List<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
