using Mastery.Client.Store.API;
using Mastery.Models.Skills;

namespace Mastery.Client.Store.Skills.Actions;

public class FetchSkillsAction
{
    private readonly SkillsApi skillsApi;

    public FetchSkillsAction(ApiProvider apiProvider)
    {
        this.skillsApi = apiProvider.SkillsApi;
    }

    public IEnumerable<SkillViewModel>? Skills { get; set; }

    public async Task FetchSkillsAsync()
    {
        this.Skills = await this.skillsApi.GetSkillsAsync();
    }
}
