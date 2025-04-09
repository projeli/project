using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Models.Requests;

public class UpdateProjectStatusRequest
{
    public ProjectStatus Status { get; set; }
}