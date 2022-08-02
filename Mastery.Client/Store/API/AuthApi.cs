using Mastery.Client.Utils;
using Mastery.Models.User;
using System.Text.Json;

namespace Mastery.Client.Store.API;

public class AuthApi : ApiBase
{
    public AuthApi(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    { }

    public async Task<UserViewModel?> GetCurrentUserAsync()
    {
        var client = this.GetApiClient();

        var authResponse = await client.GetAsync("/auth");

        using var responseStream = await authResponse.Content.ReadAsStreamAsync();

        var user = await JsonSerializer.DeserializeAsync<UserViewModel>(responseStream, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        });

        return user;
    }
}
