using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Seasons.Commands.CreateSeason;

public sealed class CreateSeasonCommandHandler : ICommandHandler<CreateSeasonCommand, Guid>
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSeasonCommandHandler(
        ISeasonRepository seasonRepository,
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _seasonRepository = seasonRepository;
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSeasonCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Get user profile
        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.UserProfile.UserProfile), userId);

        // Parse season type
        if (!Enum.TryParse<SeasonType>(request.Type, out var seasonType))
            throw new DomainException($"Invalid season type: {request.Type}");

        // Create the season
        var season = Season.Create(
            userId: userId,
            label: request.Label,
            type: seasonType,
            startDate: request.StartDate,
            expectedEndDate: request.ExpectedEndDate,
            focusRoleIds: request.FocusRoleIds,
            focusGoalIds: request.FocusGoalIds,
            successStatement: request.SuccessStatement,
            nonNegotiables: request.NonNegotiables,
            intensity: request.Intensity);

        // Save the season
        await _seasonRepository.AddAsync(season, cancellationToken);

        // Set as current season on profile
        profile.SetCurrentSeason(season);
        await _userProfileRepository.UpdateAsync(profile, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return season.Id;
    }
}
