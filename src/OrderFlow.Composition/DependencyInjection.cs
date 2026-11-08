

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OrderFlow.Application;
using OrderFlow.Infrastructure;

namespace OrderFlow.Composition;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderFlow(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddApplication()
            .AddInfrastructure(configuration);
    }
}