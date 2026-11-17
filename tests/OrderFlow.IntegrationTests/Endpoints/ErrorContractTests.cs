using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Http;

using OrderFlow.Api.Errors;
using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

/// <summary>
/// The error contract of ADR 006: a 4xx is application/problem+json with a catalog type and a traceId, even
/// when the framework produced it and no exception was thrown.
/// </summary>
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

        // JsonElement rather than ProblemDetails: traceId is an extension, not a fixed property of the standard.
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var type = problem.GetProperty("type").GetString();
        var traceId = problem.GetProperty("traceId").GetString();
        var instance = problem.GetProperty("instance").GetString();
        var detail = problem.GetProperty("detail").GetString();
        var status = problem.GetProperty("status").GetInt32();

        // EndsWith keeps the catalog's base URI out of the test.
        Assert.EndsWith("#unauthorized", type);
        Assert.Equal(MeUrl, instance);
        Assert.Equal(ProblemDetailMessages.ForStatusCode(StatusCodes.Status401Unauthorized), detail);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.False(string.IsNullOrEmpty(traceId));
    }
}