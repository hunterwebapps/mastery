using Mastery.Business.Managers;
using Mastery.Models.EventType;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class EventTypesController : ControllerBase
{
    private readonly EventTypeManager eventTypeManager;

    public EventTypesController(EventTypeManager eventTypeManager)
    {
        this.eventTypeManager = eventTypeManager;
    }

    [HttpGet]
    public async Task<ActionResult<EventTypeViewModel>> GetEventTypes()
    {
        var eventTypes = await this.eventTypeManager.GetEventTypesAsync();

        return Ok(eventTypes);
    }

    [HttpGet("{eventTypeId}", Name = nameof(GetEventType))]
    public async Task<ActionResult<EventTypeViewModel?>> GetEventType(string eventTypeId)
    {
        var eventType = await this.eventTypeManager.GetEventTypeByIdAsync(eventTypeId);

        if (eventType == null)
        {
            return NotFound();
        }

        return Ok(eventType);
    }

    [HttpPost]
    public async Task<ActionResult<EventTypeViewModel>> CreateEventType(CreateEventTypeBindingModel eventTypeBindingModel)
    {
        var eventType = await this.eventTypeManager.CreateEventTypeAsync(eventTypeBindingModel);

        return CreatedAtRoute(
            nameof(GetEventType),
            new { eventTypeId = eventType.EventTypeId },
            eventType);
    }
}
