using MassTransit;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Messages.Files;
using Projeli.Shared.Domain.Models.Files;

namespace Projeli.ProjectService.Infrastructure.Messaging.Consumers;

public class FileStoredConsumer(IProjectService projectService, IBusRepository busRepository) : IConsumer<FileStoredMessage>
{
    public async Task Consume(ConsumeContext<FileStoredMessage> context)
    {
        if (context.Message.FileType.Id.Equals(FileTypes.ProjectLogo.Id) && context.Message.ParentId is not null)
        {
            var projectId = Ulid.Parse(context.Message.ParentId);
            var userId = context.Message.UserId;

            var existingProject = await projectService.GetById(projectId, userId);

            if (!existingProject.Success || existingProject.Data is null) return;

            var result = await projectService.UpdateImageUrl(
                projectId,
                context.Message.FilePath,
                userId
            );

            if (!result.Success || existingProject.Data.ImageUrl is null) return;

            await busRepository.Publish(new FileDeleteMessage
            {
                FilePath = existingProject.Data.ImageUrl,
                FileType = FileTypes.ProjectLogo,
                ParentId = projectId.ToString(),
                UserId = userId
            });
        }
    }
}