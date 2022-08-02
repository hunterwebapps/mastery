using Mastery.Models.Quest;
using System.Net.Http.Json;

namespace Mastery.Client.Store.API;

public class QuestApi : ApiBase
{
    public QuestApi(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    { }

    public async Task<QuestViewModel?> CreateQuestAsync(CreateQuestBindingModel createQuestBindingModel)
    {
        var client = this.GetApiClient();

        var response = await client.PostAsJsonAsync("/quests", createQuestBindingModel);

        var quest = await this.DeserializeResponseAsync<QuestViewModel>(response);

        return quest;
    }
}
