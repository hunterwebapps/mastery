using Mastery.DataAccess;
using Mastery.DataAccess.Entities;
using Mastery.Models.EventType;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Business.Managers;

public class EventTypeManager
{
    private readonly SqlDbContext dbContext;

    public EventTypeManager(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<EventTypeViewModel?> GetEventTypeByIdAsync(string eventTypeId)
    {
        var entity = await this.dbContext.EventTypes.FindAsync(eventTypeId);

        if (entity == null)
        {
            return null;
        }

        return new EventTypeViewModel(
            entity.EventTypeId,
            entity.Name,
            entity.Description,
            entity.ImageUrl);
    }

    public async Task<IEnumerable<EventTypeViewModel>> GetEventTypesAsync()
    {
        var entities = await this.dbContext.EventTypes.ToListAsync();

        return entities.Select(x =>
            new EventTypeViewModel(
                x.EventTypeId,
                x.Name,
                x.Description,
                x.ImageUrl));
    }

    public async Task<EventTypeViewModel> CreateEventTypeAsync(CreateEventTypeBindingModel eventTypeBindingModel)
    {
        var entity = new EventType()
        {
            Name = eventTypeBindingModel.Name,
            Description = eventTypeBindingModel.Description,
            ImageUrl = eventTypeBindingModel.ImageUrl,
        };

        await this.dbContext.AddAsync(entity);

        await this.dbContext.SaveChangesAsync();

        return new EventTypeViewModel(
            entity.EventTypeId,
            entity.Name,
            entity.Description,
            entity.ImageUrl);
    }
}
