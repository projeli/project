using MassTransit;
using Projeli.Shared.Application.Messages.Files;
using Projeli.Shared.Domain.Models.Files;

namespace Projeli.ProjectService.Infrastructure.Messaging.Consumers;

public class FileStoreFailedConsumer : IConsumer<FileStoreFailedMessage>
{
    public Task Consume(ConsumeContext<FileStoreFailedMessage> context)
    {
        if (context.Message.FileType.Id.Equals(FileTypes.ProjectLogo.Id))
        {
            Console.WriteLine($"Failed to store file for project: {context.Message.ParentId}, Error: {string.Join(", ", context.Message.ErrorMessages)}");
        }

        return Task.CompletedTask;
    }
}