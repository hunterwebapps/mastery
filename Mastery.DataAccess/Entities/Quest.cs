namespace Mastery.DataAccess.Entities;
public class Quest
{
    public string QuestId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public User User { get; set; } = default!;
    public IEnumerable<Event> Events { get; set; } = Enumerable.Empty<Event>();
}
