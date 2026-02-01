using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Seasons.Models;
using Mastery.Domain.Entities;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Seasons.Queries.GetUserSeasons;

public sealed class GetUserSeasonsQueryHandler : IQueryHandler<GetUserSeasonsQuery, IReadOnlyList<SeasonDto>>
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUserSeasonsQueryHandler(
        ISeasonRepository seasonRepository,
        ICurrentUserService currentUserService)
    {
        _seasonRepository = seasonRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<SeasonDto>> Handle(GetUserSeasonsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        var seasons = await _seasonRepository.GetByUserIdAsync(userId, cancellationToken);

        return seasons.Select(MapToDto).ToList();
    }

    private static SeasonDto MapToDto(Season season)
    {
        return new SeasonDto
        {
            Id = season.Id,
            UserId = season.UserId,
            Label = season.Label,
            Type = season.Type.ToString(),
            StartDate = season.StartDate,
            ExpectedEndDate = season.ExpectedEndDate,
            ActualEndDate = season.ActualEndDate,
            FocusRoleIds = season.FocusRoleIds.ToList(),
            FocusGoalIds = season.FocusGoalIds.ToList(),
            SuccessStatement = season.SuccessStatement,
            NonNegotiables = season.NonNegotiables.ToList(),
            Intensity = season.Intensity,
            Outcome = season.Outcome,
            IsEnded = season.IsEnded,
            CreatedAt = season.CreatedAt,
            ModifiedAt = season.ModifiedAt
        };
    }
}
