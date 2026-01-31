using Mastery.Domain.Diagnostics.Snapshots;

namespace Mastery.Application.Common.Interfaces;

public interface IUserStateAssembler
{
    Task<UserStateSnapshot> AssembleAsync(string userId, CancellationToken cancellationToken = default);
}
