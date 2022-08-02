using Mastery.DataAccess;
using Mastery.DataAccess.Entities;
using Mastery.Models.Quest;

namespace Mastery.Business.Managers;

public class QuestManager
{
    private readonly SqlDbContext dbContext;

    public QuestManager(SqlDbContext sqlDbContext)
    {
        this.dbContext = sqlDbContext;
    }

    public async Task<QuestViewModel?> GetQuestByIdAsync(string questId)
    {
        var entity = await this.dbContext.Quests.FindAsync(questId);

        if (entity == null)
        {
            return null;
        }

        return new QuestViewModel(
            entity.QuestId,
            entity.Title,
            entity.Description,
            entity.UserId,
            entity.Events.Select(x => x.EventId));
    }

    public async Task<QuestViewModel> CreateQuestAsync(string userId, CreateQuestBindingModel createQuestBindingModel)
    {
        // TODO: Validate conflicting fields don't already exist.

        var questId = Guid.NewGuid().ToString();

        var eventIds = createQuestBindingModel
            .EventBindingModels
            .Select(x => Guid.NewGuid().ToString())
            .ToArray();

        // TODO: Test that this will always be in the same order.
        var events = createQuestBindingModel.EventBindingModels.Select((x, i) =>
        {
            var eventId = eventIds[i];

            var decisions = x.DecisionBindingModels.Select((y, i) =>
            {
                var decisionId = Guid.NewGuid().ToString();

                var activities = y.ActivityBindingModels.Select(z =>
                {
                    return new Activity()
                    {
                        ActivityId = Guid.NewGuid().ToString(),
                        ActivityTypeId = z.ActivityTypeId,
                        DecisionId = decisionId,
                        NextEventId = z.NextEventIndex.HasValue
                            ? eventIds[z.NextEventIndex.Value]
                            : null,
                    };
                });

                return new Decision()
                {
                    DecisionId = decisionId,
                    Description = y.Description,
                    SortOrder = i,
                    EventId = eventId,
                    PotentialActivities = activities,
                };
            });

            return new Event()
            {
                EventId = eventId,
                QuestId = questId,
                EventTypeId = x.EventTypeId,
                Decisions = decisions,
            };
        });

        var quest = new Quest()
        {
            QuestId = questId,
            Title = createQuestBindingModel.Title,
            Description = createQuestBindingModel.Description,
            UserId = userId,
            Events = events,
        };

        await this.dbContext.Quests.AddAsync(quest);

        await this.dbContext.SaveChangesAsync();

        return new QuestViewModel(
            quest.QuestId,
            quest.Title,
            quest.Description,
            quest.UserId,
            quest.Events.Select(x => x.EventId));
    }
}
