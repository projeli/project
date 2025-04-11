using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Projeli.ProjectService.Api.Extensions;

public static class OpenTelemetryExtension
{
    public static void AddProjectServiceOpenTelemetry(this IServiceCollection services, ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        loggingBuilder.AddOpenTelemetry(builder =>
        {
            builder.IncludeScopes = true;
            builder.IncludeFormattedMessage = true;
        });
        
        services.AddOpenTelemetry()
            .ConfigureResource(builder => builder.AddService("project-service"))
            .WithMetrics(builder =>
            {
                builder
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .WithTracing(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation();
            });
        
        var useOtlpExporter = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        
        if (useOtlpExporter)
        {
            services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }
        
        services.AddOpenTelemetry().WithMetrics(x => x.AddPrometheusExporter());

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }
    
    public static void UseProjectServiceOpenTelemetry(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        
        app.MapHealthChecks("/health");
        
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
    }
}