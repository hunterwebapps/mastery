namespace Mastery.Api.Workers;

public sealed class BackgroundWorkerOptions
{
    public const string SectionName = "BackgroundWorker";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 3;
    public int MaxUsersPerRun { get; set; } = 100;
    public int TimeoutMinutesPerUser { get; set; } = 2;
}
