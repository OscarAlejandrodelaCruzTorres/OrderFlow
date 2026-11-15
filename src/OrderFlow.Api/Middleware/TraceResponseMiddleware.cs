using Microsoft.AspNetCore.Http.Features;

using OrderFlow.Api.Observability;

namespace OrderFlow.Api.Middleware;

/// <summary>
/// Returns the request's trace id on every response, in the <c>traceresponse</c> header. Error responses already
/// carry it in the ProblemDetails body; this covers the successful ones too, so a 200 with wrong data can be
/// reported to support and found in the logs and traces like any error.
/// </summary>
public sealed class TraceResponseMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Read on the way in, from the activity ASP.NET Core created for this request: by the time the headers are
        // sent, Activity.Current could be another span (an outgoing HttpClient call, for example).
        var traceId = context.Features.Get<IHttpActivityFeature>()?.Activity.Id ?? context.TraceIdentifier;

        // Once the response starts the headers can no longer change, and it can start anywhere below: a controller,
        // the exception handler, a 401 or a 429. OnStarting runs right before the headers go out, whoever sends them.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceHeaders.TraceResponse] = traceId;
            return Task.CompletedTask;
        });

        // Nothing to do after the rest of the pipeline, so no async state machine: hand the task back as it is.
        return next(context);
    }
}