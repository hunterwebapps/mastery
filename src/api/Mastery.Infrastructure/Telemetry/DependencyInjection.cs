using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mastery.Infrastructure.Telemetry;

public static class TelemetryDependencyInjection
{
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApplicationInsightsOptions>(
            configuration.GetSection(ApplicationInsightsOptions.SectionName));

        services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();

        return services;
    }
}
