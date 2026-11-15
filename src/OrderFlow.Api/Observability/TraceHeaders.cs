namespace OrderFlow.Api.Observability;

/// <summary>
/// Names of the headers that carry the trace id back to the client. Shared by the middleware that writes them,
/// the CORS policy that exposes them and the tests, so a typo in any of the three fails at compile time instead
/// of silently breaking the contract.
/// </summary>
public static class TraceHeaders
{
    // The W3C Trace Context (level 2) counterpart of `traceparent`, sent on every response, not only on errors,
    // so a successful request can be reported to support too. Lowercase, as HTTP/2 sends every header name.
    public const string TraceResponse = "traceresponse";
}