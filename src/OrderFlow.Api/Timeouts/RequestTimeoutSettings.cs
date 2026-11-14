using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Api.Timeouts;

/// <summary>
/// Default request timeout (section "RequestTimeout"), validated at startup. Capped at five minutes: anything
/// longer belongs in a 202 Accepted operation (ADR 009), not in a longer timeout.
/// </summary>
public sealed class RequestTimeoutSettings
{
    public const string SectionName = "RequestTimeout";

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan Default { get; init; } = TimeSpan.FromSeconds(30);
}