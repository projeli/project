using ProjectService.Domain.Models;

namespace ProjectService.Application.Dtos;

public class ProjectLinkDto
{
    public Ulid Id { get; set; }
    public Ulid ProjectId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public ProjectLinkType Type { get; set; }
}