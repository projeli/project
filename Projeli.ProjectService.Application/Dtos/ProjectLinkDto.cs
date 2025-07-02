using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Dtos;

public class ProjectLinkDto
{
    public Ulid Id { get; set; }
    public Ulid ProjectId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public ProjectLinkType Type { get; set; }
    public ushort Order { get; set; }
}