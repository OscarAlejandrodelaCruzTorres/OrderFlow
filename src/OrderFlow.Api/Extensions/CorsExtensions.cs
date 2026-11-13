using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using OrderFlow.Api.Cors;

namespace OrderFlow.Api.Extensions;

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
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(settings.Value.AllowedOrigins)
                    .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete, HttpMethods.Patch)
                    .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType, HeaderNames.IfMatch, "Idempotency-Key")
                    .WithExposedHeaders(HeaderNames.RetryAfter, "api-supported-versions");
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