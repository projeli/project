using MassTransit;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.Shared.Domain.Models.Files;
using Projeli.Shared.Infrastructure.Messaging.Events;

namespace Projeli.ProjectService.Infrastructure.Messaging.Consumers;

public class FileStoredConsumer(IProjectService projectService, IBus bus) : IConsumer<FileStoredEvent>
{
    public async Task Consume(ConsumeContext<FileStoredEvent> context)
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

            await bus.Publish(new FileDeleteEvent
            {
                FilePath = existingProject.Data.ImageUrl,
                FileType = FileTypes.ProjectLogo,
                ParentId = projectId.ToString(),
                UserId = userId
            });
        }
    }
}