using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

/// <summary>
/// The 429 path, where OnRejected (status and Retry-After, no body) and UseStatusCodePages (the ProblemDetails)
/// must cooperate. A copy of the factory with PermitLimit = 1 keeps the low limit away from the other tests.
/// </summary>
public sealed class RateLimitingTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    private const string MeUrl = "/api/v1/me";

    [Fact]
    public async Task SecondRequestOverTheLimitReturns429WithRetryAfterAndProblemDetails()
    {
        // A copy of the shared factory with a limit of one request per window.
        using var limitedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:PermitLimit"] = "1" })));

        using var client = limitedFactory.CreateClientFor(TestTokens.Create("user-123", "Laura Perez", "TenantUser"));

        var first = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        var second = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);

        // OnRejected writes no body, so this proves UseStatusCodePages turned the 429 into ProblemDetails.
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);

        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.EndsWith("#too-many-requests", problem.GetProperty("type").GetString());

        // Rounded up in OnRejected: a truncated value could be zero and make the client retry at once.
        Assert.NotNull(second.Headers.RetryAfter?.Delta);
        Assert.True(second.Headers.RetryAfter.Delta > TimeSpan.Zero);
    }
}