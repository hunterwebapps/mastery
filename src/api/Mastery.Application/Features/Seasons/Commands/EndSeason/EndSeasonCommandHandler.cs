using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Seasons.Commands.EndSeason;

public sealed class EndSeasonCommandHandler : ICommandHandler<EndSeasonCommand>
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public EndSeasonCommandHandler(
        ISeasonRepository seasonRepository,
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _seasonRepository = seasonRepository;
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EndSeasonCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Get the season
        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken)
            ?? throw new NotFoundException(nameof(Season), request.SeasonId);

        // Verify the season belongs to the current user
        if (season.UserId != userId)
            throw new DomainException("You do not have permission to end this season.");

        // Check if already ended
        if (season.IsEnded)
            throw new DomainException("This season has already ended.");

        // End the season
        var endDate = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        season.End(endDate, request.Outcome);

        await _seasonRepository.UpdateAsync(season, cancellationToken);

        // Clear current season from profile if this is the current season
        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile?.CurrentSeasonId == request.SeasonId)
        {
            profile.ClearCurrentSeason();
            await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
