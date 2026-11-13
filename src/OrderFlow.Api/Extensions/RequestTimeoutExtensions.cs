using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Options;

using OrderFlow.Api.Timeouts;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// A default timeout for every request, taken from RequestTimeoutSettings and validated at startup.
/// </summary>
public static class RequestTimeoutExtensions
{
    public static IServiceCollection AddApiRequestTimeouts(this IServiceCollection services)
    {
        services.AddOptions<RequestTimeoutSettings>()
            .BindConfiguration(RequestTimeoutSettings.SectionName, binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Cancels the request's token once the limit passes and answers 504. It only works where the token is
        // honoured, and it never fires with a debugger attached.
        services.AddRequestTimeouts();

        // Deferred: the settings are read when the middleware first asks for its options, after the container
        // exists and the settings have been validated. Registering services cannot resolve IOptions<T> yet.
        services.AddOptions<RequestTimeoutOptions>()
            .Configure<IOptions<RequestTimeoutSettings>>((options, settings) =>
                options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = settings.Value.Default });

        return services;
    }
}