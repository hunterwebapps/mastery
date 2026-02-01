namespace Mastery.Application.Features.Seasons.Models;

/// <summary>
/// Season DTO.
/// </summary>
public sealed record SeasonDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? ExpectedEndDate { get; init; }
    public DateOnly? ActualEndDate { get; init; }
    public List<Guid> FocusRoleIds { get; init; } = [];
    public List<Guid> FocusGoalIds { get; init; } = [];
    public string? SuccessStatement { get; init; }
    public List<string> NonNegotiables { get; init; } = [];
    public int Intensity { get; init; }
    public string? Outcome { get; init; }
    public bool IsEnded { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Season summary for list views.
/// </summary>
public sealed record SeasonSummaryDto
{
    public Guid Id { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? ActualEndDate { get; init; }
    public bool IsEnded { get; init; }
    public int Intensity { get; init; }
}
