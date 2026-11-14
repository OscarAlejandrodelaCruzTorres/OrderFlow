using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OrderFlow.Api.OpenApi;

/// <summary>
/// Declares the JWT Bearer scheme in the OpenAPI document, which is what makes
/// Swagger UI offer the Authorize button and send the token. It only describes
/// the API; enforcement stays in the authorization policies.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        // Declared for the whole document because the fallback policy protects
        // every endpoint by default.
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        });

        return Task.CompletedTask;
    }
}