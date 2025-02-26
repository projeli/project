using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Dtos;

public class ProjectDto
{
    public Ulid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ProjectCategory Category { get; set; }
    public List<ProjectMemberDto> Members { get; set; } = [];
    public List<ProjectLinkDto> Links { get; set; } = [];
    public List<ProjectTagDto> Tags { get; set; } = [];
}