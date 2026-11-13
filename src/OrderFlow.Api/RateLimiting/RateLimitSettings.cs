using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Api.RateLimiting;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    [Range(1, 10_000)]
    public int PermitLimit { get; init; } = 60;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
}