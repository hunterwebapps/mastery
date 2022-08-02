namespace Mastery.DataAccess.Entities;
public class Event
{
    public string EventId { get; set; } = default!;
    public string EventTypeId { get; set; } = default!;
    public EventType EventType { get; set; } = default!;
    public string QuestId { get; set; } = default!;
    public Quest Quest { get; set; } = default!;
    public IEnumerable<Decision> Decisions { get; set; } = Enumerable.Empty<Decision>();
}
