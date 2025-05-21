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
            x.AddConsumer<ProjectSyncRequestConsumer>();
            x.AddConsumer<FileStoreFailedConsumer>();
            x.AddConsumer<FileStoredConsumer>();
            x.AddConsumer<UserDeletedConsumer>();
            
            x.UsingRabbitMq((context, config) =>
            {
                config.Host(configuration["RabbitMq:Host"] ?? throw new MissingEnvironmentVariableException("RabbitMq:Host"), "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? throw new MissingEnvironmentVariableException("RabbitMq:Username"));
                    h.Password(configuration["RabbitMq:Password"] ?? throw new MissingEnvironmentVariableException("RabbitMq:Password"));
                });
                
                config.ReceiveEndpoint("project-project-sync-request-queue", e =>
                {
                    e.ConfigureConsumer<ProjectSyncRequestConsumer>(context);
                });
                
                config.ReceiveEndpoint("project-file-store-failed-queue", e =>
                {
                    e.ConfigureConsumer<FileStoreFailedConsumer>(context);
                });
                
                config.ReceiveEndpoint("project-file-stored-queue", e =>
                {
                    e.ConfigureConsumer<FileStoredConsumer>(context);
                });
                
                config.ReceiveEndpoint("project-user-deleted-queue", e =>
                {
                    e.ConfigureConsumer<UserDeletedConsumer>(context);
                });
                
                config.PublishFanOut<ProjectCreatedEvent>();
                config.PublishFanOut<ProjectSyncEvent>();
                config.PublishFanOut<ProjectUpdatedEvent>();
                config.PublishFanOut<ProjectDeletedEvent>();
                config.PublishFanOut<FileStoreEvent>();
                config.PublishFanOut<FileDeleteEvent>();
            });
        });
    }

    private static void PublishFanOut<T>(this IRabbitMqBusFactoryConfigurator configurator)
        where T : class
    {
        configurator.Publish<T>(y => y.ExchangeType = "fanout");
    }
}