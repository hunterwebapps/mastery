using Mastery.Client.Store.API;

namespace Mastery.Client.Store;

public class ApiProvider
{
    public ApiProvider(IHttpClientFactory httpClientFactory)
    {
        this.AuthApi = new(httpClientFactory);
        this.QuestApi = new(httpClientFactory);
        this.SkillsApi = new(httpClientFactory);
    }

    public AuthApi AuthApi;
    public QuestApi QuestApi;
    public SkillsApi SkillsApi;
}
