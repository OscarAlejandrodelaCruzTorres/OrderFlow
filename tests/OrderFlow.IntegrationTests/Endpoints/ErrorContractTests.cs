

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

public sealed class ErrorContractTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    private const string MeUrl = "/api/v1/me";


    [Fact]
    public async Task UnauthorizedReturnsProblemDetailsWithTypeAndTraceId()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var type = problem.GetProperty("type").GetString();
        var traceId = problem.GetProperty("traceId").GetString();
        Assert.EndsWith("#unauthorized", type);
        Assert.False(string.IsNullOrEmpty(traceId));

    }

}