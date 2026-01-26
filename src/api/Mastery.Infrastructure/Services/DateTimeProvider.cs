using Mastery.Application.Common.Interfaces;

namespace Mastery.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
