using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

using Microsoft.Extensions.Options;

using OrderFlow.Api.RateLimiting;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// Per-user rate limiting. Limits come from RateLimitSettings, validated at startup, and a rejection answers
/// 429 with Retry-After and the same ProblemDetails as every other error.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitSettings>()
            .BindConfiguration(RateLimitSettings.SectionName, binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // The lease knows when the window resets; a fixed value would make clients wait too long or retry
            // too soon. No body is written here, so UseStatusCodePages answers with ProblemDetails.
            options.OnRejected = (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            // One window per user, so a noisy client cannot use up everyone else's quota. The settings are read
            // from RequestServices because the container does not exist yet while services are being registered.
            options.AddPolicy(RateLimitPolicies.PerUser, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    // The subject (sub), not the name: names repeat and change, sub is unique and stable. In F3 it
                    // is combined with the tenant, so the same user id in two tenants does not share a quota.
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = settings.Window
                    });
            });
        });

        return services;
    }
}