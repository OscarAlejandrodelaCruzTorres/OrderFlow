using System.Net;

using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

/// <summary>
/// The probes must answer without a token: orchestrators call them anonymously, and a protected liveness
/// probe would make the platform restart a healthy container. Readiness starts depending on Azure SQL in F1.
/// </summary>
public sealed class HealthTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    private const string LiveUrl = "/health/live";
    private const string ReadyUrl = "/health/ready";
    private const string StartupUrl = "/health/startup";

    [Fact]
    public async Task LiveWithoutTokenReturns200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(LiveUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyWithoutTokenReturns200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ReadyUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StartupWithoutTokenReturns200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(StartupUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}