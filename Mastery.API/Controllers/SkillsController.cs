using Mastery.Business.Managers;
using Mastery.Models.Skills;
using Microsoft.AspNetCore.Mvc;

namespace Mastery.API.Controllers;
[Route("[controller]")]
[ApiController]
public class SkillsController : ControllerBase
{
    private readonly SkillsManager skillsManager;

    public SkillsController(SkillsManager skillsManager)
    {
        this.skillsManager = skillsManager;
    }

    [HttpGet("{skillId}", Name = nameof(GetSkill))]
    public async Task<ActionResult<SkillViewModel?>> GetSkill(int skillId)
    {
        var model = await this.skillsManager.GetSkillByIdAsync(skillId);

        return Ok(model);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SkillViewModel>>> GetSkills()
    {
        var skills = await this.skillsManager.GetSkillsAsync();

        return Ok(skills);
    }

    [HttpPost]
    public async Task<ActionResult<SkillViewModel?>> CreateSkill(CreateSkillBindingModel createSkillBindingModel)
    {
        var model = await this.skillsManager.CreateSkillAsync(createSkillBindingModel);

        if (model == null)
        {
            return BadRequest();
        }

        return CreatedAtRoute(
            nameof(GetSkill),
            new { skillId = model.SkillId },
            model);
    }

    [HttpPost]
    public async Task<ActionResult<string>> UploadImage()
    {
        var file = Request.Form.Files[0];

        if (file.Length == 0)
        {
            return BadRequest();
        }

        var imageUrl = await this.skillsManager.SaveImageAsync(file);

        return Ok(imageUrl);
    }
}
