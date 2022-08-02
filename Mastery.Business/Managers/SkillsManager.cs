using Mastery.DataAccess;
using Mastery.DataAccess.Entities;
using Mastery.Models.Skills;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Business.Managers;

public class SkillsManager
{
    private readonly SqlDbContext dbContext;

    public SkillsManager(SqlDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<SkillViewModel?> GetSkillByIdAsync(int skillId)
    {
        var entity = await this.dbContext.Skills.FindAsync(skillId);

        if (entity == null)
        {
            return null;
        }

        return new SkillViewModel(
            entity.SkillId,
            entity.Name,
            entity.Description,
            entity.ImageUrl);
    }

    public async Task<IEnumerable<SkillViewModel>> GetSkillsAsync()
    {
        var skills = await this.dbContext.Skills.ToListAsync();

        var viewModels = skills.Select(x => new SkillViewModel(
                x.SkillId,
                x.Name,
                x.Description,
                x.ImageUrl));

        return viewModels;
    }

    public async Task<SkillViewModel?> CreateSkillAsync(CreateSkillBindingModel createSkillBindingModel)
    {
        // TODO: Check for conflicts.

        var entity = new Skill()
        {
            SkillId = Guid.NewGuid().ToString(),
            Name = createSkillBindingModel.Name,
            Description = createSkillBindingModel.Description,
            ImageUrl = createSkillBindingModel.ImageUrl,
        };

        await this.dbContext.AddAsync(entity);

        await this.dbContext.SaveChangesAsync();

        return new SkillViewModel(
            entity.SkillId,
            entity.Name,
            entity.Description,
            entity.ImageUrl);
    }

    public Task<string> SaveImageAsync(IFormFile formFile)
    {
        return Task.FromResult("");
    }
}
