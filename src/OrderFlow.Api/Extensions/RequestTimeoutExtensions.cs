using Microsoft.AspNetCore.Http.Timeouts;
namespace OrderFlow.Api.Extensions;

public static class RequestTimeoutExtensions
{
    public static IServiceCollection AddApiRequestTimeouts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRequestTimeouts(options =>
        options.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = configuration.GetValue("RequestTimeout", TimeSpan.FromSeconds(30))
        });

        return services;
    }
}