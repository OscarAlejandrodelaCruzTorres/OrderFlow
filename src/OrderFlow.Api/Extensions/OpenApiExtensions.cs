using System.Text.Json.Serialization;

using Asp.Versioning;

using OrderFlow.Api.OpenApi;
using OrderFlow.Api.Versioning;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// URL-segment versioning (/api/v1/...) and one OpenAPI document per version. The Bearer transformer only
/// describes the security for Swagger; enforcement stays in the authorization policies.
/// </summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        // The OpenAPI generator reads HttpJsonOptions, not MVC's: without this the document would describe enums as
        // numbers while the API sends names.
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddApiVersioning(options =>
            {
                // The version is read from the route only; reading several places costs every request (AV0015).
                options.ApiVersionReader = new UrlSegmentApiVersionReader();

                // Adds api-supported-versions (and api-deprecated-versions) to every versioned response.
                options.ReportApiVersions = true;
            })
            // Versions are declared once for all controllers instead of per class.
            .AddMvc(options => options.Conventions.Add(new GlobalApiVersionConvention()))
            .AddApiExplorer(options =>
            {
                // Document names "v1", "v2"..., and /api/v1/... instead of /api/v{version}/... in the document.
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options => options.Document.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }
}