using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateValues;

public sealed class UpdateValuesCommandHandler : ICommandHandler<UpdateValuesCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateValuesCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateValuesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var values = request.Values.Select(v => new UserValue
        {
            Id = v.Id == Guid.Empty ? Guid.NewGuid() : v.Id,
            Key = v.Key,
            Label = v.Label,
            Rank = v.Rank,
            Weight = v.Weight,
            Notes = v.Notes,
            Source = v.Source ?? "manual"
        });

        profile.UpdateValues(values);

        await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
