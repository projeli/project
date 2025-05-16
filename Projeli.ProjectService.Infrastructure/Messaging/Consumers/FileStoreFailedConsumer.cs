using MassTransit;
using Projeli.Shared.Domain.Models.Files;
using Projeli.Shared.Infrastructure.Messaging.Events;

namespace Projeli.ProjectService.Infrastructure.Messaging.Consumers;

public class FileStoreFailedConsumer : IConsumer<FileStoreFailedEvent>
{
    public Task Consume(ConsumeContext<FileStoreFailedEvent> context)
    {
        if (context.Message.FileType.Id.Equals(FileTypes.ProjectLogo.Id))
        {
            Console.WriteLine($"Failed to store file for project: {context.Message.ParentId}, Error: {string.Join(", ", context.Message.ErrorMessages)}");
        }

        return Task.CompletedTask;
    }
}