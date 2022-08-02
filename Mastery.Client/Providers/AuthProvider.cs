using Mastery.Client.Store;
using Mastery.Client.Store.API;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Mastery.Client.Providers;

public class AuthProvider : AuthenticationStateProvider
{
    private readonly AuthApi authApi;

    public AuthProvider(ApiProvider apiProvider)
    {
        this.authApi = apiProvider.AuthApi;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await this.authApi.GetCurrentUserAsync();

        var identity = new ClaimsIdentity();

        if (user != null)
        {
            identity.AddClaims(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
            });            
        }

        var principal = new ClaimsPrincipal(identity);

        var state = new AuthenticationState(principal);

        NotifyAuthenticationStateChanged(Task.FromResult(state));

        return state;
    }
}
