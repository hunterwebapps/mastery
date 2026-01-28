namespace Mastery.Infrastructure.Services;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxOutputTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}
