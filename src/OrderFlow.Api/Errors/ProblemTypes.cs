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
}
