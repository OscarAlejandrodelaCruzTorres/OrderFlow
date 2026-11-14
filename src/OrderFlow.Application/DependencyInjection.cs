using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Application;

/// <summary>
/// Registers the Application layer: use-case services and validators. The host calls it through
/// Composition and never names its types.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Nothing yet: the first services arrive with the catalog in F1.
        return services;
    }
}