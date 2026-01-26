using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateRoles;

public sealed class UpdateRolesCommandHandler : ICommandHandler<UpdateRolesCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRolesCommandHandler(
        IUserProfileRepository userProfileRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateRolesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var roles = request.Roles.Select(r => new UserRole
        {
            Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id,
            Key = r.Key,
            Label = r.Label,
            Rank = r.Rank,
            SeasonPriority = r.SeasonPriority,
            MinWeeklyMinutes = r.MinWeeklyMinutes,
            TargetWeeklyMinutes = r.TargetWeeklyMinutes,
            Tags = r.Tags,
            Status = Enum.TryParse<RoleStatus>(r.Status, out var status) ? status : RoleStatus.Active
        });

        profile.UpdateRoles(roles);

        await _userProfileRepository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
