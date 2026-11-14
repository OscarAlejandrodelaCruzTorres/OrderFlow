namespace OrderFlow.Application.Health;

/// <summary>
/// Tags that decide which health checks each probe runs. They live in Application because both sides need
/// them: Infrastructure tags its checks, the Api filters by them.
/// </summary>
public static class HealthTags
{
    public const string Ready = "ready";
    public const string Startup = "startup";
}