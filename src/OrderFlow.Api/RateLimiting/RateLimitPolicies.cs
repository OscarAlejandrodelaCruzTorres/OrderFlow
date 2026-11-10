namespace OrderFlow.Api.RateLimiting;

/// <summary>
/// Names of the rate limiting policies, shared by their registration and the
/// endpoints that apply them, so a typo fails at compile time instead of at runtime.
/// </summary>
public static class RateLimitPolicies
{
    public const string PerUser = "PerUser";
}
