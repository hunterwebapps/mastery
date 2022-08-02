namespace Mastery.DataAccess.Entities;
public class Decision
{
    public string DecisionId { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int SortOrder { get; set; } = default!;
    public string EventId { get; set; } = default!;
    public virtual Event Event { get; set; } = default!;
    public virtual IEnumerable<Activity> PotentialActivities { get; set; } = Enumerable.Empty<Activity>();
}
