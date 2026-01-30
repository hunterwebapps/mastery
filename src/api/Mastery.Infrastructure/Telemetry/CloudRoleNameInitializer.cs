using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Telemetry;

public class CloudRoleNameInitializer(IOptions<ApplicationInsightsOptions> options)
    : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = options.Value.CloudRoleName;
    }
}
