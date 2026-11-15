using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using OrderFlow.Api.Cors;
using OrderFlow.Api.Observability;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// CORS for browser clients only: origins come from CorsSettings, validated at startup. Non-browser clients
/// (curl, other services) are not affected by CORS at all.
/// </summary>
public static class CorsExtensions
{
    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        services.AddOptions<CorsSettings>()
            .BindConfiguration(CorsSettings.SectionName, binder => binder.ErrorOnUnknownConfiguration = true)
            .Validate(settings => settings.AllowedOrigins.All(IsValidOrigin),
                "Every Cors:AllowedOrigins entry must be an origin: http(s)://host[:port], without path or trailing slash.")
            .ValidateOnStart();

        services.AddCors();

        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((options, settings) =>
            {
                // Only what exists today: GET and the Authorization header. Write methods and their headers
                // (Content-Type, If-Match, Idempotency-Key) are added in F6 with the endpoints that need them.
                // Tokens travel in the Authorization header, so credentials stay disabled. The browser hides any
                // response header not listed here from the client's code, so Retry-After, api-supported-versions
                // and traceresponse (the trace id to report to support) are exposed explicitly.
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(settings.Value.AllowedOrigins)
                        .WithMethods(HttpMethods.Get)
                        .WithHeaders(HeaderNames.Authorization, "traceparent", "tracestate")
                        .WithExposedHeaders(HeaderNames.RetryAfter, "api-supported-versions", TraceHeaders.TraceResponse);
                });
            });

        return services;
    }

    // An origin is scheme + host + optional port, nothing else: "localhost:4200" parses as a URI with scheme
    // "localhost", and "http://localhost:4200/" would never match the browser's Origin header.
    private static bool IsValidOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase);
}