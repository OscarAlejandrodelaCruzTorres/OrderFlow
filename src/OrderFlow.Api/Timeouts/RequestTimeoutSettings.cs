using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Api.Timeouts;

public sealed class RequestTimeoutSettings
{
    public const string SectionName = "RequestTimeout";

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan Default { get; init; } = TimeSpan.FromSeconds(30);
}