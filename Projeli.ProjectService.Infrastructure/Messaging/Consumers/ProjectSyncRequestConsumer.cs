using MassTransit;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.Shared.Domain.Results;
using Projeli.Shared.Infrastructure.Messaging.Events;

namespace Projeli.ProjectService.Infrastructure.Messaging.Consumers;

public class ProjectSyncRequestConsumer : IConsumer<ProjectSyncRequestEvent>
{
    public async Task Consume(ConsumeContext<ProjectSyncRequestEvent> context)
    {
        var projectService = context.GetServiceOrCreateInstance<IProjectService>();
        IResult<ProjectDto?> existingProject;

        if (context.Message.ProjectId.HasValue && context.Message.ProjectId.Value != default)
        {
            existingProject = await projectService.GetById(context.Message.ProjectId.Value, null, true);
        }
        else if (!string.IsNullOrWhiteSpace(context.Message.ProjectSlug))
        {
            existingProject = await projectService.GetBySlug(context.Message.ProjectSlug!, null, true);
        }
        else
        {
            existingProject = Result<ProjectDto?>.NotFound();
        }

        if (existingProject.Data is not null)
        {
            var bus = context.GetServiceOrCreateInstance<IBus>();

            await bus.Publish(new ProjectSyncEvent
            {
                ProjectId = existingProject.Data.Id,
                ProjectName = existingProject.Data.Name,
                ProjectSlug = existingProject.Data.Slug,
                Members = existingProject.Data.Members.Select(x => new ProjectSyncEvent.ProjectMember
                {
                    UserId = x.UserId,
                    IsOwner = x.IsOwner
                }).ToList()
            });
        }
    }
}