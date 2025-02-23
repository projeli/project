using ProjectService.Domain.Models;

namespace ProjectService.Application.Models.Requests;

public class CreateProjectRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; }
    public ProjectCategory? Category { get; set; }
    public string[] Tags { get; set; } = [];
}