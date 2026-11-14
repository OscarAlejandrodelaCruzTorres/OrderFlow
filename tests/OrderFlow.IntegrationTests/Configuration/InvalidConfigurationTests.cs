using Microsoft.Extensions.Configuration;

using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Configuration;

/// <summary>
/// Options are validated at startup (ValidateOnStart): a bad setting must stop the host instead of
/// reaching production as a silently wrong limit.
/// </summary>
public sealed class InvalidConfigurationTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    [Theory]
    [InlineData("RateLimiting:PermitLimit", "0", "PermitLimit")]
    [InlineData("RequestTimeout:Default", "00:00:00", "Default")]
    [InlineData("Cors:AllowedOrigins:0", "localhost:4200", "Cors:AllowedOrigins")]
    [InlineData("RateLimiting:PerLimit", "3", "PerLimit")]
    public void InvalidSettingStopsTheHost(string key, string value, string expectedInMessage)
    {
        // A copy of the shared factory with one bad setting added last, so it wins.
        using var invalidFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));

        // The host starts on the first client. ThrowsAny because each validation throws its own type and it may
        // arrive wrapped; the message check proves the host stopped for this setting and not another reason.
        var exception = Assert.ThrowsAny<Exception>(() => invalidFactory.CreateClient());

        Assert.Contains(expectedInMessage, exception.ToString(), StringComparison.Ordinal);
    }
}