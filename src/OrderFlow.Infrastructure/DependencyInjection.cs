using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Infrastructure;

/// <summary>
/// Registers persistence and external services. It receives the configuration because this layer owns the
/// connections to the outside world (Azure SQL from F1); the other layers get typed options instead.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Nothing yet: the DbContexts and their health checks arrive in F1.
        return services;
    }
}