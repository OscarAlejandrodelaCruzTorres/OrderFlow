using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OrderFlow.Api.Errors;

public sealed partial class GlobalExceptionHandler : IExceptionHandler
{

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int statusCode;
        string title;
        string type;

        if(exception is OperationCanceledException)
        {
            statusCode = StatusCodes.Status504GatewayTimeout;
            title = "The request timed out";
            type = ProblemTypes.Timeout;

            LogRequestCancelled(_logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            title = "An unexpected error occurred";
            type = ProblemTypes.Internal;

            LogUnhandledException(_logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type
            }
            });
    }
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Method} {Path} was cancelled server-side.")]
    private static partial void LogRequestCancelled(ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, PathString path);
}

