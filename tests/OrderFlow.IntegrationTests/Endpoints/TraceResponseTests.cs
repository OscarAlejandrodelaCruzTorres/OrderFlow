using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using OrderFlow.Api.Observability;
using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

/// <summary>
/// Every response carries its trace id in the traceresponse header, successful or not, and on an error it is the
/// same id the ProblemDetails body reports, so any request can be traced back from what the client received.
/// </summary>
public sealed class TraceResponseTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    private const string MeUrl = "/api/v1/me";

    [Fact]
    public async Task SuccessfulResponseCarriesTraceResponseHeader()
    {
        using var client = factory.CreateClientFor(TestTokens.Create("user-123", "Laura Perez", "TenantUser"));

        var response = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        // Status first: a 401 also carries the header, so without this the test could pass for the wrong reason.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // response.Headers, not response.Content.Headers: it describes the response, not the body.
        Assert.True(response.Headers.TryGetValues(TraceHeaders.TraceResponse, out var values));
        var traceResponse = Assert.Single(values);
        Assert.False(string.IsNullOrEmpty(traceResponse));
    }

    [Fact]
    public async Task ErrorResponseTraceResponseMatchesProblemTraceId()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(TraceHeaders.TraceResponse, out var values));
        var traceResponse = Assert.Single(values);

        // The header comes from TraceResponseMiddleware and the body from the problem details service: two separate
        // paths that must report the same id, or support would be handed one that leads nowhere.
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(problem.GetProperty("traceId").GetString(), traceResponse);
    }
}