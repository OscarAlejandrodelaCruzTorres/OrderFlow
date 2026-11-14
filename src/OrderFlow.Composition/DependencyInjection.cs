using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OrderFlow.Application;
using OrderFlow.Infrastructure;

namespace OrderFlow.Composition;

/// <summary>
/// The composition root: the only project that sees both Application and Infrastructure. The Api calls
/// AddOrderFlow once and never names a repository, a DbContext or a database provider.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddOrderFlow(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddApplication()
            .AddInfrastructure(configuration)
            // Code asks TimeProvider for the time instead of DateTime.UtcNow, so tests can control it.
            .AddSingleton(TimeProvider.System);
    }
}