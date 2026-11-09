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

                if (problemDetails.Type is null || problemDetails.Type.StartsWith("https://tools.ietf.org/", StringComparison.Ordinal))
                {
                    problemDetails.Type = ProblemTypes.GetProblemTypeForStatusCode(problemDetails.Status) ?? problemDetails.Type;
                }
            };
        });
        return services;
    }
}