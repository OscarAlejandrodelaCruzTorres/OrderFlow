using System.Net;
using System.Net.Http.Json;

using OrderFlow.Application.Contracts.Users;
using OrderFlow.IntegrationTests.Support;

namespace OrderFlow.IntegrationTests.Endpoints;

/// <summary>
/// Authentication and authorization through the real pipeline: fallback policy, the PlatformAdmin
/// policy and claim mapping, with tokens signed by the test key.
/// </summary>
public sealed class AuthorizationTests(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>
{
    private const string MeUrl = "/api/v1/me";
    private const string PingUrl = "/api/admin/ping";

    [Fact]
    public async Task MeWithoutTokenReturns401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MeWithTokenReturnsTheCallerClaims()
    {
        using var client = factory.CreateClient(TestTokens.Create("user-123", "Laura Perez", "TenantUser"));

        var response = await client.GetAsync(MeUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(me);
        Assert.Equal("user-123", me.Sub);
        Assert.Equal("Laura Perez", me.Name);
        Assert.Equal(["TenantUser"], me.Roles);
    }

    [Fact]
    public async Task PingWithoutTokenReturns401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(PingUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PingAsTenantUserReturns403()
    {
        using var client = factory.CreateClient(TestTokens.Create("user-123", "Laura Perez", "TenantUser"));

        var response = await client.GetAsync(PingUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PingAsPlatformAdminReturns204()
    {
        using var client = factory.CreateClient(TestTokens.Create("user-123", "Laura Perez", "PlatformAdmin"));

        var response = await client.GetAsync(PingUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}