namespace OrderFlow.Api.Errors;

/// <summary>
/// A generic, human-readable explanation per status code, for the ProblemDetails the framework produces.
/// Specific errors (a broken business rule, for example) set their own detail and keep it. English only:
/// clients translate from the problem type, never from this text.
/// </summary>
public static class ProblemDetailMessages
{
    /// <summary>
    /// The default detail for a status code, or null when there is none, so the caller leaves it empty.
    /// </summary>
    public static string? ForStatusCode(int? statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "One or more fields are invalid. See errors for details.",
            StatusCodes.Status401Unauthorized => "A valid bearer token is required.",
            StatusCodes.Status403Forbidden => "Your account does not have the permission this endpoint requires.",
            StatusCodes.Status404NotFound => "No resource matches this route.",
            StatusCodes.Status405MethodNotAllowed => "This route does not support the HTTP method used.",
            StatusCodes.Status409Conflict => "The request conflicts with the current state of the resource.",
            StatusCodes.Status429TooManyRequests => "Too many requests. Wait the number of seconds in the Retry-After header.",
            StatusCodes.Status500InternalServerError => "An unexpected error occurred. If it persists, report the traceId to support.",
            StatusCodes.Status504GatewayTimeout => "The request took longer than the server allows.",
            _ => null
        };
}