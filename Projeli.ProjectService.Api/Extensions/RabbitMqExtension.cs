using MassTransit;
using Projeli.ProjectService.Infrastructure.Messaging.Consumers;
using Projeli.Shared.Application.Messages.Files;
using Projeli.Shared.Application.Messages.Projects;
using Projeli.Shared.Application.Messages.Projects.Members;
using Projeli.Shared.Infrastructure.Exceptions;

namespace Projeli.ProjectService.Api.Extensions;

public static class RabbitMqExtension
{
    public static void UseProjectServiceRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
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
                
                config.PublishFanOut<ProjectCreatedMessage>();
                config.PublishFanOut<ProjectUpdatedDetailsMessage>();
                config.PublishFanOut<ProjectUpdatedOwnershipMessage>();
                config.PublishFanOut<ProjectMemberAddedMessage>();
                config.PublishFanOut<ProjectMemberRemovedMessage>();
                config.PublishFanOut<ProjectDeletedMessage>();
                config.PublishFanOut<FileStoreMessage>();
                config.PublishFanOut<FileDeleteMessage>();
            });
        });
    }

    private static void PublishFanOut<T>(this IRabbitMqBusFactoryConfigurator configurator)
        where T : class
    {
        configurator.Publish<T>(y => y.ExchangeType = "fanout");
    }
}