using Mastery.Client.Utils;
using System.Text.Json;

namespace Mastery.Client.Store.API;

public abstract class ApiBase
{
    private readonly IHttpClientFactory httpClientFactory;

    public ApiBase(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage httpResponse)
    {
        var contentStream = await httpResponse.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<T>(
            contentStream,
            new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
            });
    }

    protected HttpClient GetApiClient() => this.httpClientFactory.CreateClient(HttpClientNames.API);
}
