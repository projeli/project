using System.Reflection;
using MassTransit;
using Projeli.ProjectService.Infrastructure.Messaging.Consumers;
using Projeli.Shared.Infrastructure.Exceptions;
using Projeli.Shared.Infrastructure.Messaging.Events;

namespace Projeli.ProjectService.Api.Extensions;

public static class RabbitMqExtension
{
    public static void UseProjectServiceRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, config) =>
            {
                config.Host(configuration["RabbitMq:Host"] ?? throw new MissingEnvironmentVariableException("RabbitMq:Host"), "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? throw new MissingEnvironmentVariableException("RabbitMq:Username"));
                    h.Password(configuration["RabbitMq:Password"] ?? throw new MissingEnvironmentVariableException("RabbitMq:Password"));
                });

                config.ConfigureEndpoints(context);
                
                config.ReceiveEndpoint("project-project-sync-request-queue", e =>
                {
                    e.ConfigureConsumer<ProjectSyncRequestConsumer>(context);
                });
                
                config.PublishFanOut<ProjectCreatedEvent>();
                config.PublishFanOut<ProjectSyncEvent>();
                config.PublishFanOut<ProjectUpdatedEvent>();
            });

            x.AddConsumers(Assembly.GetAssembly(typeof(ProjectSyncRequestConsumer)));
        });
    }

    private static void ReceiveEndpoint<T>(this IRabbitMqBusFactoryConfigurator configurator, string queueName)
        where T : class, IConsumer, new()
    {
        configurator.ReceiveEndpoint("project-" + queueName, e => { e.Consumer<T>(); });
    }

    private static void PublishFanOut<T>(this IRabbitMqBusFactoryConfigurator configurator)
        where T : class
    {
        configurator.Publish<T>(y => y.ExchangeType = "fanout");
    }
}