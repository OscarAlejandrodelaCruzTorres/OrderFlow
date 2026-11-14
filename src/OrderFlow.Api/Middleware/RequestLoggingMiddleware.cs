using System.Diagnostics;

namespace OrderFlow.Api.Middleware;

/// <summary>
/// Logs the method, path, final status code, duration and trace id of every request, at a level that follows
/// the status code. The query string is left out so its values never reach the logs.
/// </summary>
public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        catch
        {
            // The exception handler deals with almost everything, so this only sees what it let through.
            // Without a log here that request would leave no line at all. The exception itself is logged further in.
            var statusCode = context.Response.HasStarted
                ? context.Response.StatusCode
                : StatusCodes.Status500InternalServerError;

            Log(context, start, statusCode, LogLevel.Error);

            throw;
        }

        Log(context, start, context.Response.StatusCode, LevelFor(context.Response.StatusCode));
    }

    // Checked first: the elapsed time and the trace id are only computed when the line will be written.
    private void Log(HttpContext context, long start, int statusCode, LogLevel level)
    {
        if (logger.IsEnabled(level))
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var traceId = TraceIdOf(context);

            LogRequest(logger, level, context.Request.Method, context.Request.Path, statusCode, elapsedMs, traceId);
        }
    }

    private static LogLevel LevelFor(int statusCode) => statusCode switch
    {
        >= StatusCodes.Status500InternalServerError => LogLevel.Error,
        >= StatusCodes.Status400BadRequest => LogLevel.Warning,
        _ => LogLevel.Information
    };

    // Same value as the traceId in the ProblemDetails, so an id reported by a client can be searched in the logs.
    private static string TraceIdOf(HttpContext context) => Activity.Current?.Id ?? context.TraceIdentifier;

    [LoggerMessage(Message = "{Method} {Path} -> {StatusCode} in {ElapsedMs:F0} ms (trace {TraceId})")]
    private static partial void LogRequest(ILogger logger, LogLevel level, string method, PathString path,
        int statusCode, double elapsedMs, string traceId);
}