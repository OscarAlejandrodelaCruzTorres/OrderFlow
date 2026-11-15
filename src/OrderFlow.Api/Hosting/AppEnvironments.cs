namespace OrderFlow.Api.Hosting;

/// <summary>
/// Custom environment names, shared by the host and the integration tests so they can't drift apart:
/// a typo in one of them would silently turn off the checks that only run in that environment.
/// </summary>
public static class AppEnvironments
{
    public const string Testing = "Testing";
}