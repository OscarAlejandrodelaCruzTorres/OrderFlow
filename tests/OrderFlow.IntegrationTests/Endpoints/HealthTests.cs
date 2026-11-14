
using System.Net;

using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

public sealed class HealthTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    private const string LiveUrl = "/health/live";
    private const string StartupUrl = "/health/startup";
    private const string ReadyUrl = "/health/ready";


    [Fact]
    public async Task LiveWithoutTokenResultReturns200()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(LiveUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyWithoutTokenResultReturns200()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(ReadyUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]

    public async Task StartupWithoutTokenResultReturns200()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(StartupUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}