namespace Mastery.DataAccess.Entities;
public class EventType
{
    public string EventTypeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string ImageUrl { get; set; } = default!;
    public virtual IEnumerable<Event> Events { get; set; } = Enumerable.Empty<Event>();
}
