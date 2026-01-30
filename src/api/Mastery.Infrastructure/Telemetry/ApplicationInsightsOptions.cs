namespace Mastery.Infrastructure.Telemetry;

public sealed class ApplicationInsightsOptions
{
    public const string SectionName = "ApplicationInsights";
    public string ConnectionString { get; set; } = string.Empty;
    public string InstrumentationKey { get; set; } = string.Empty;
    public string CloudRoleName { get; set; } = "Mastery.Api";
    public bool EnableAdaptiveSampling { get; set; } = true;
}
