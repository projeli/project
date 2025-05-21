using MassTransit;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.Shared.Application.Messages.Users;

namespace Projeli.ProjectService.Infrastructure.Messaging.Consumers;

public class UserDeletedConsumer(IProjectService projectService, IProjectMemberService projectMemberService) : IConsumer<UserDeletedMessage>
{
    public async Task Consume(ConsumeContext<UserDeletedMessage> context)
    {
        var projectsResult = await projectService.GetByUserId(context.Message.UserId);

        if (projectsResult is { Success: true, Data: not null })
        {
            foreach (var project in projectsResult.Data)
            {
                if (project.Members.Count > 1)
                {
                    var projectMember = project.Members.FirstOrDefault(m => m.UserId == context.Message.UserId);
                    if (projectMember is not null)
                    {
                        if (projectMember.IsOwner)
                        {
                            var newOwner = project.Members
                                .OrderByDescending(member => (int)member.Permissions)
                                .FirstOrDefault(member => member.UserId != context.Message.UserId);
                            
                            if (newOwner is null) 
                            {
                                await projectService.Delete(project.Id, context.Message.UserId);
                                continue;
                            }
                            
                            await projectService.UpdateOwnership(project.Id, newOwner.UserId, context.Message.UserId);
                        }

                        await projectMemberService.Delete(project.Id, context.Message.UserId, null, true);
                    }
                }
                else
                {
                    await projectService.Delete(project.Id, context.Message.UserId);
                }
            }
        }
        else
        {
            Console.WriteLine(
                $"Failed to get projects for user {context.Message.UserId}: {projectsResult.Errors.Values.FirstOrDefault()}");
        }
    }
}