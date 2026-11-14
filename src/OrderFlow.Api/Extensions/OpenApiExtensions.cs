using Asp.Versioning;

using OrderFlow.Api.OpenApi;
using OrderFlow.Api.Versioning;

namespace OrderFlow.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        // Implementation for adding API documentation
        services.AddApiVersioning(options =>
        {
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            options.ReportApiVersions = true;
        })
        .AddMvc(options => options.Conventions.Add(new GlobalApiVersionConvention()))
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        })
        .AddOpenApi(options =>
        options.Document.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        return services;
    }
}