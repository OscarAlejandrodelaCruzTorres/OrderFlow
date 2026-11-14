namespace OrderFlow.Api.Cors;

/// <summary>
/// Browser origins allowed to call the API (section "Cors"), validated at startup. Empty means no
/// cross-origin browser client is allowed.
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}