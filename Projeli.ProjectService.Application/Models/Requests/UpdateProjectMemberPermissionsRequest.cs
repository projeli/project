using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Models.Requests;

public class UpdateProjectMemberPermissionsRequest
{
    public ProjectMemberPermissions Permissions { get; set; }
}