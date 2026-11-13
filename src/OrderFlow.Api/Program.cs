using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using OrderFlow.Api.Extensions;
using OrderFlow.Api.RateLimiting;
using OrderFlow.Application.Health;
using OrderFlow.Composition;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOrderFlow(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddApiErrors();
builder.Services.AddApiDocumentation();
builder.Services.AddApiSecurity();
builder.Services.AddApiRateLimiting();
builder.Services.AddApiObservability(builder.Configuration);
builder.Services.AddApiRequestTimeouts();
builder.Services.AddApiCors();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();
app.UseRequestTimeouts();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthTags.Ready)
}).AllowAnonymous();

app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthTags.Startup)
}).AllowAnonymous();

app.MapControllers().RequireRateLimiting(RateLimitPolicies.PerUser);

app.Run();