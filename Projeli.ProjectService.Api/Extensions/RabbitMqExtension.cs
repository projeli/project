using System.Reflection;
using MassTransit;
using Projeli.ProjectService.Infrastructure.Messaging.Consumers;
using Projeli.Shared.Infrastructure.Messaging.Events;

namespace Projeli.ProjectService.Api.Extensions;

public static class RabbitMqExtension
{
    public static void UseProjectServiceRabbitMq(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, config) =>
            {
                config.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                config.ConfigureEndpoints(context);

                config.ReceiveEndpoint<ProjectSyncRequestConsumer>("project-sync-request-queue");

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
        configurator.ReceiveEndpoint("wiki-" + queueName, e => { e.Consumer<T>(); });
    }

    private static void PublishFanOut<T>(this IRabbitMqBusFactoryConfigurator configurator)
        where T : class
    {
        configurator.Publish<T>(y => y.ExchangeType = "fanout");
    }
}