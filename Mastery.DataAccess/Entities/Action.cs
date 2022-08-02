namespace Mastery.DataAccess.Entities;
public class ActivityType
{
    public int ActivityTypeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IEnumerable<Activity> DecisionActions { get; set; } = Enumerable.Empty<Activity>();
}
