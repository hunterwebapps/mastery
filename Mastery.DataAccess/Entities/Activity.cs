namespace Mastery.DataAccess.Entities;
public class Activity
{
    public string ActivityId { get; set; } = default!;
    // Unique Constraint or Composite Key for DecisionId and ActionTypeId? Or could there be duplicate actions per event?
    public string DecisionId { get; set; } = default!;
    public virtual Decision Decision { get; set; } = default!;
    public int ActivityTypeId { get; set; } = default!;
    public virtual ActivityType ActivityType { get; set; } = default!;
    public string? NextEventId { get; set; }
    public virtual Event? NextEvent { get; set; }
}
