namespace Mastery.Application.Common.Interfaces;

public interface IMasteryDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
