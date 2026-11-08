namespace OrderFlow.Api.Errors;

public static class ProblemTypes
{
    private const string Base =
        "https://github.com/OscarAlejandrodelaCruzTorres/OrderFlow/blob/main/docs/errores.md#";

    public const string Validation = Base + "validation";
    public const string Unauthorized = Base + "unauthorized";
    public const string Forbidden = Base + "forbidden";
    public const string NotFound = Base + "not-found";
    public const string Conflict = Base + "conflict";
    public const string PreconditionFailed = Base + "precondition-failed";
    public const string PreconditionRequired = Base + "precondition-required";
    public const string BusinessRule = Base + "business-rule";
    public const string IdempotencyConflict = Base + "idempotency-conflict";
    public const string TooManyRequests = Base + "too-many-requests";
    public const string Timeout = Base + "timeout";
    public const string Internal = Base + "internal";

    public static string? GetProblemTypeForStatusCode(int? statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => Validation,
            StatusCodes.Status401Unauthorized => Unauthorized,
            StatusCodes.Status403Forbidden => Forbidden,
            StatusCodes.Status404NotFound => NotFound,
            StatusCodes.Status409Conflict => Conflict,
            StatusCodes.Status412PreconditionFailed => PreconditionFailed,
            StatusCodes.Status428PreconditionRequired => PreconditionRequired,
            StatusCodes.Status422UnprocessableEntity => BusinessRule,
            StatusCodes.Status429TooManyRequests => TooManyRequests,
            StatusCodes.Status504GatewayTimeout => Timeout,
            StatusCodes.Status500InternalServerError => Internal,
            _ => null
        };
}