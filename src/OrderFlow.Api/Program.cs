using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using OrderFlow.Api.Extensions;
using OrderFlow.Api.RateLimiting;
using OrderFlow.Application.Health;
using OrderFlow.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// One call composes the layers below: this project never names a repository, a DbContext or a database
// provider. The rest is host concerns, one extension per topic in Extensions/.
builder.Services
    .AddOrderFlow(builder.Configuration)
    .AddApiErrors()
    .AddApiDocumentation()
    .AddApiSecurity()
    .AddApiRateLimiting()
    .AddApiObservability(builder.Configuration)
    .AddApiRequestTimeouts()
    .AddApiCors();

var app = builder.Build();

// Outermost, so it covers every middleware registered below and turns any exception into ProblemDetails.
app.UseExceptionHandler();

// Gives a ProblemDetails body to errors that are not exceptions (401, 403, 404, 405, 429).
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Anonymous because the fallback policy would otherwise cover the documents. One document per API version.
    app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();

    // Swagger UI only: the documents come from Microsoft.AspNetCore.OpenApi, so Swashbuckle generates nothing.
    // New versions appear here on their own.
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", description.GroupName);
        }

        options.DocumentTitle = "OrderFlow API";
    });
}
else
{
    // Outside Development only: browsers would remember HTTPS-only for localhost and get in the way locally.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Before authentication: preflight requests carry no token, and the 401/403 responses need the CORS headers
// for the browser to let the client read them.
app.UseCors();

// Who the caller is, then what they may do.
app.UseAuthentication();
app.UseAuthorization();

// Partitioned per user, which is why the limiter runs after authorization.
app.UseRateLimiter();

// Cancels the request's token after the configured limit and answers 504. Cooperative: it only stops code
// that passes the token down to I/O, and it never fires with a debugger attached.
app.UseRequestTimeouts();

// Anonymous because orchestrators probe without a token; the response is only the overall status.
// Not rate limited: only the controllers below require the limiter.

// Liveness runs no checks: an Azure SQL outage must not make the orchestrator restart a healthy container.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();

// Readiness runs the checks tagged "ready" (Azure SQL from F1); while it fails, the instance gets no traffic.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthTags.Ready)
}).AllowAnonymous();

// Startup holds the other probes back until a slow first start finishes.
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthTags.Startup)
}).AllowAnonymous();

app.MapControllers().RequireRateLimiting(RateLimitPolicies.PerUser);

app.Run();