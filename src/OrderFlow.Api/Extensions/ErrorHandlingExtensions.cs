using OrderFlow.Api.Errors;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// How the API turns failures into ProblemDetails: one handler for exceptions and a
/// catalog type for the errors the framework produces. Where this runs in the
/// pipeline is decided in Program.
/// </summary>
public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddApiErrors(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var problemDetails = context.ProblemDetails;

                // The path identifies this occurrence (RFC 9457). The query string is left out, as in the request log,
                // so its values never reach the client. ??= keeps a more specific instance set by our own code.
                problemDetails.Instance ??= context.HttpContext.Request.Path;

                // A generic explanation per status for the errors the framework produces. ??= keeps the specific detail our
                // own code sets, like the business-rule message of a 422 in F6.
                problemDetails.Detail ??= ProblemDetailMessages.ForStatusCode(problemDetails.Status);

                // Development only: the real exception message, to debug faster. Never in any other environment.
                var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                if (environment.IsDevelopment() && context.Exception is not null)
                {
                    problemDetails.Detail = context.Exception.Message;
                }

                // Only replace the framework's generic RFC links; a type set by our own code is kept.

                if (problemDetails.Type is null || problemDetails.Type.StartsWith("https://tools.ietf.org/", StringComparison.Ordinal))
                {
                    problemDetails.Type = ProblemTypes.GetProblemTypeForStatusCode(problemDetails.Status) ?? problemDetails.Type;
                }
            };
        });

        return services;
    }
}