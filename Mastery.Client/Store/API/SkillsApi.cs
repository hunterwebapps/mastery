using Mastery.Models.Skills;
using System.Net.Http.Json;

namespace Mastery.Client.Store.API;

public class SkillsApi : ApiBase
{
    public SkillsApi(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    { }

    public async Task<IEnumerable<SkillViewModel>?> GetSkillsAsync()
    {
        var client = this.GetApiClient();

        var response = await client.GetAsync("/skills");

        var skills = await this.DeserializeResponseAsync<IEnumerable<SkillViewModel>>(response);

        return skills;
    }

    public async Task<SkillViewModel?> CreateSkillAsync(CreateSkillBindingModel createSkillBindingModel)
    {
        var client = this.GetApiClient();

        var response = await client.PostAsJsonAsync("/skills", createSkillBindingModel);

        var skill = await this.DeserializeResponseAsync<SkillViewModel>(response);

        return skill;
    }
}
