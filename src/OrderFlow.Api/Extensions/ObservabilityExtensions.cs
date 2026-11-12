using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OrderFlow.Api.Extensions;

/// <summary>
/// What the API reports about itself: traces, metrics and logs through OpenTelemetry,
/// and the health checks that orchestrators probe. Exporting is opt-in by configuration.
/// </summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddApiObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("OrderFlow.Api"))
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(options =>
                // Probes arrive every few seconds and would bury the traces worth reading.
                options.Filter = context => !context.Request.Path.StartsWithSegments("/health")))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging();

        // Opt-in: with no collector to talk to, exporting would only produce failed
        // connections. Protocol and headers come from the standard OTEL_EXPORTER_OTLP_*
        // settings, so changing backend needs no code.
        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            openTelemetry.UseOtlpExporter();
        }

        // The checks themselves are registered by the layers that own what they check
        // (Infrastructure adds Azure SQL with HealthTags.Ready in F1).
        services.AddHealthChecks();

        return services;
    }
}