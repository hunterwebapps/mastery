namespace Mastery.Models.Quest;
public record QuestViewModel(
    string QuestId,
    string Title,
    string Description,
    string UserId,
    IEnumerable<string> EventIds);
