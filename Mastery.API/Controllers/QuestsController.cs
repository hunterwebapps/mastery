using Mastery.API.Extensions;
using Mastery.Business.Managers;
using Mastery.Models.Quest;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class QuestsController : ControllerBase
{
    private readonly QuestManager questManager;

    public QuestsController(QuestManager questManager)
    {
        this.questManager = questManager;
    }

    [HttpGet("{questId}", Name = nameof(GetQuest))]
    public async Task<ActionResult<QuestViewModel>> GetQuest(string questId)
    {
        var questViewModel = await this.questManager.GetQuestByIdAsync(questId);

        if (questViewModel == null)
        {
            return NotFound();
        }

        return Ok(questViewModel);
    }

    [HttpPost]
    public async Task<ActionResult<QuestViewModel>> CreateQuest(CreateQuestBindingModel createQuestBindingModel)
    {
        var userId = this.User.GetUserId();

        var questViewModel = await this.questManager.CreateQuestAsync(userId, createQuestBindingModel);

        return CreatedAtRoute(
            nameof(GetQuest),
            new { questId = questViewModel.QuestId },
            questViewModel);
    }
}
