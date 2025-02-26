using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Dtos;

public class ProjectMemberDto
{
    public Ulid Id { get; set; }
    public Ulid ProjectId { get; set; }
    public string UserId { get; set; }
    public bool IsOwner { get; set; }
    public string Role { get; set; }
    public ProjectMemberPermissions Permissions { get; set; }
}