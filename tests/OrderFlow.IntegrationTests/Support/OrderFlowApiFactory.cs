
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.IntegrationTests.Support;

/// <summary>
/// Hosts the real API in memory for the tests: same pipeline and configuration as production, except
/// that tokens are validated against the test key and the rate limit is raised so tests don't throttle.
/// </summary>
public sealed class OrderFlowApiFactory : WebApplicationFactory<Program>
{
    // HTTPS so UseHttpsRedirection has nothing to redirect and HSTS behaves as in production.
    public OrderFlowApiFactory() => ClientOptions.BaseAddress = new Uri("https://localhost");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Not Development: no user secrets, no user-jwts settings, no Swagger.
        builder.UseEnvironment("Testing");

        // 2. Test settings, added after appsettings.json, so they win.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "1000"
            }));

        // 3. Trust tokens signed with the test key. Runs after the app's own configuration.
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = TestTokens.Issuer;
                options.TokenValidationParameters.ValidAudience = TestTokens.Audience;
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.Key;
            }));
    }
}