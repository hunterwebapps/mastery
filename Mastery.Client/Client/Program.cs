using Fluxor;
using Mastery.Client.Providers;
using Mastery.Client.Store;
using Mastery.Client.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace Mastery.Client;
public class Program
{
    public static async Task Main(string[] args)
    {

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddHttpClient(HttpClientNames.API, client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
        });

        // Plugins
        builder.Services.AddMudServices();
        builder.Services.AddFluxor(x =>
        {
            x.ScanAssemblies(typeof(Program).Assembly);
            x.UseReduxDevTools(rdx => rdx.Name = "Mastery");
        });

        // Dependency Injection
        builder.Services.AddSingleton<ApiProvider>();
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddScoped<AuthenticationStateProvider, AuthProvider>();

        builder.Services.AddAuthorizationCore();

        await builder.Build().RunAsync();
    }
}
